using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nes.Core.Control.Agreement;

public class DataFrame
{
    public const ushort DefaultDataFrameVersion = 0x01; // 默认数据帧版本
    public const int DataFrameHeaderLength = 11; // 数据帧头部长度

    /// <summary>
    /// 协议版本(2字节)
    /// </summary>
    public ushort Version
    {
        get;
        private set
        {
            field = value;
            m_MS.Seek(0, SeekOrigin.Begin);
            m_MS.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));
        }
    }

    /// <summary>
    /// 数据类型(1字节)
    /// </summary>
    public DataFrameType Type
    {
        get;
        set
        {
            field = value;
            m_MS.Seek(2, SeekOrigin.Begin);
            m_MS.WriteByte((byte)value);
        }
    }

    /// <summary>
    /// 数据帧序列号(4字节)
    /// </summary>
    public int SequenceNumber
    {
        get;
        set
        {
            field = value;
            m_MS.Seek(3, SeekOrigin.Begin);
            m_MS.Write(BitConverter.GetBytes(value), 0, sizeof(int));
        }
    }

    /// <summary>
    /// 数据长度(4字节)
    /// </summary>
    public int DataLength
    {
        get;
        private set
        {
            field = value;
            m_MS.Seek(7, SeekOrigin.Begin);
            m_MS.Write(BitConverter.GetBytes(value), 0, sizeof(int));
        }
    }

    /// <summary>
    /// 数据内容
    /// </summary>
    public byte[] Data
    {
        set
        {
            m_MS.Seek(11, SeekOrigin.Begin);
            m_MS.Write(value, 0, value.Length);
            DataLength = value.Length;
            Checksum = CalculateChecksum(value);
        }
    }

    /// <summary>
    /// 数据部分的CRC校验和(4字节)
    /// </summary>
    public uint Checksum
    {
        get;
        private set
        {
            field = value;
            m_MS.Seek(11 + DataLength, SeekOrigin.Begin);
            m_MS.Write(BitConverter.GetBytes(value), 0, sizeof(uint));
        }
    }

    /// <summary>
    /// 数据帧缓冲区
    /// </summary>
    public byte[] Buffer => m_MS.ToArray( ); // 数据帧缓冲区

    private readonly MemoryStream m_MS; // 数据流

    public DataFrame(ushort version, DataFrameType type, int sequenceNumber, byte[] data)
    {
        m_MS = new MemoryStream(15 + data.Length);
        Version = version;
        Type = type;
        SequenceNumber = sequenceNumber;
        Data = data;
    }

    public DataFrame(DataFrameType type, int sequenceNumber, MemoryStream stream)
    {
        m_MS = new MemoryStream(15 + (int)stream.Length);
        Version = DefaultDataFrameVersion;
        Type = type;
        SequenceNumber = sequenceNumber;
        DataLength = (int)stream.Length;
        m_MS.Write(stream.GetBuffer( ), 0, DataLength);
        Checksum = CalculateChecksum(stream.GetBuffer( ), 0, DataLength);
    }

    public DataFrame(DataFrameType type, int sequenceNumber, byte[] data)
        : this(DefaultDataFrameVersion, type, sequenceNumber, data)
    { }

    public DataFrame(in byte[] buffer)
    {
        m_MS = new MemoryStream(buffer, 0, buffer.Length, true, true);
        Version = BitConverter.ToUInt16(buffer, 0);
        Type = (DataFrameType)buffer[2];
        SequenceNumber = BitConverter.ToInt32(buffer, 3);
        DataLength = BitConverter.ToInt32(buffer, 7);
        Checksum = BitConverter.ToUInt32(buffer, 11 + DataLength);

        if(Checksum != CalculateChecksum(buffer, 11, DataLength))
        {
            m_MS.Dispose( );    // 释放资源
            m_MS = new(DataFrameHeaderLength + 4);  // 设置新数据流
            Version = 0;
            Type = DataFrameType.None;
            SequenceNumber = 0;
            DataLength = 0;
            Checksum = 0;
        }
    }

    ~DataFrame( )
    {
        m_MS.Dispose( );    // 释放资源
    }

    /// <summary>
    /// 将数据帧发送到流
    /// </summary>
    public void Send(Stream stream)
    {
        m_MS.Seek(0, SeekOrigin.Begin);
        m_MS.CopyTo(stream);
    }

    /// <summary>
    /// 将数据帧发送到流
    /// </summary>
    public async Task SendAsync(Stream stream)
    {
        m_MS.Seek(0, SeekOrigin.Begin);
        await m_MS.CopyToAsync(stream);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetBuffer( )
    {
        return m_MS.GetBuffer( );
    }

    /// <summary>
    /// 将数据帧发送到字节数组
    /// </summary>
    /// <param name="buffer">接收数据的字节数组</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">数组空间不足</exception>
    public void Send(byte[] buffer)
    {
        if(buffer.Length < m_MS.Length) throw new IndexOutOfRangeException("数组空间不足!!!");
        using var stream = new MemoryStream(buffer);
        m_MS.Seek(0, SeekOrigin.Begin);
        m_MS.CopyTo(stream);
    }

    /// <summary>
    /// 将数据帧发送到字节数组
    /// </summary>
    /// <param name="buffer">接收数据的字节数组</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">数组空间不足</exception>
    public async Task SendAsync(byte[] buffer)
    {
        if(buffer.Length < m_MS.Length) throw new IndexOutOfRangeException("数组空间不足!!!");
        using var stream = new MemoryStream(buffer);
        m_MS.Seek(0, SeekOrigin.Begin);
        await m_MS.CopyToAsync(stream);
    }

    /// <summary>
    /// 计算数据帧CRC32校验和
    /// </summary>
    public static uint CalculateChecksum(byte[] data, int offset = 0, int length = -1)
    {
        if(length == -1) length = data.Length;
        uint crc = 0xFFFFFFFF;
        for(int i = 0; i < length; i++)
        {
            crc ^= (uint)(data[offset + i] << 24);
            for(int j = 0; j < 8; j++)
            {
                if((crc & 0x80000000) != 0)
                    crc = (crc << 1) ^ 0x04C11DB7;
                else
                    crc <<= 1;
            }
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// 解析数据帧
    /// </summary>
    /// <param name="frame">数据帧</param>
    /// <returns>依据数据帧格式返回数据</returns>
    public object? Analyze( )
    {
        using BinaryReader reader = new(m_MS);
        reader.BaseStream.Seek(DataFrameHeaderLength, SeekOrigin.Begin);   // 跳过数据帧头部
        switch(Type)
        {
            case DataFrameType.ConnectionRequest:   // 连接请求
                var buffer = reader.ReadBytes(6);
                var agreementCode = Encoding.UTF8.GetString(buffer);    // 协议码
                var connectCount = reader.ReadInt32( ); // 连接次数
                return (agreementCode, connectCount);

            case DataFrameType.ConnectionResponse:  // 连接响应
                connectCount = reader.ReadInt32( ); // 连接次数
                return connectCount;

            case DataFrameType.OpenGameRequest: // 打开游戏请求
                buffer = reader.ReadBytes(DataLength);
                var gameName = Encoding.UTF8.GetString(buffer); // 游戏名称
                return gameName;

            case DataFrameType.OpenGameResponse:    // 打开游戏响应
                var isSuccess = reader.ReadBoolean( );  // 是否成功
                buffer = reader.ReadBytes(DataLength - sizeof(bool));
                gameName = Encoding.UTF8.GetString(buffer); // 游戏名称
                return (isSuccess, gameName);

            case DataFrameType.PauseGameRequest:    // 暂停游戏请求
                return null;

            case DataFrameType.PauseGameResponse:   // 暂停游戏响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.ResumeGameRequest:   // 恢复游戏请求
                return null;

            case DataFrameType.ResumeGameResponse:  // 恢复游戏响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.StopGameRequest: // 关闭游戏请求
                return null;

            case DataFrameType.StopGameResponse:    // 关闭游戏响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.OperationSyncRequest:   // 操作数据同步
                buffer = reader.ReadBytes(DataLength);
                return buffer;

            case DataFrameType.OperationSyncResponse:   // 操作数据同步响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.ImageDataRequest:  // 图像数据请求
                var num = reader.ReadUInt32( );   // 图像数据序号
                buffer = reader.ReadBytes(DataLength - sizeof(uint));
                return (num, buffer);

            case DataFrameType.ImageDataResponse: // 图像数据响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.AudioDataRequest:  // 音频数据请求
                return (m_MS.GetBuffer( ), DataFrameHeaderLength, DataLength);

            case DataFrameType.AudioDataResponse: // 音频数据响应
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            case DataFrameType.NesFileInfosRequest:  // 文件信息请求
                var fileCount = reader.ReadInt32( );  // 文件数量
                var infos = new (int MapperNum, string Name)[fileCount];
                for(int i = 0; i < fileCount; i++)
                {
                    infos[i].MapperNum = reader.ReadInt32( );  // Mapper
                    infos[i].Name = reader.ReadString( );  // 文件名
                }
                return infos;

            case DataFrameType.NesFileInfosResponse:
                isSuccess = reader.ReadBoolean( );  // 是否成功
                return isSuccess;

            default:
                return null;
        }
    }
}

/// <summary>
/// 数据帧类型
/// </summary>
public enum DataFrameType : byte
{
    /// <summary>
    /// 连接请求
    /// <para>[约定码(6<see langword="byte"/>)、连接次数(<see langword="int"/>)]</para>
    /// </summary>
    ConnectionRequest,

    /// <summary>
    /// 连接响应
    /// <para>[连接次数(<see langword="int"/>)]</para>
    /// </summary>
    ConnectionResponse,

    /// <summary>
    /// 打开游戏请求
    /// <para>[游戏名称(<see langword="string"/>)]</para>
    /// </summary>
    OpenGameRequest,

    /// <summary>
    /// 打开游戏响应
    /// <para>[(是否成功(<see langword="bool"/>), 游戏名称(<see langword="string"/>))]</para>
    /// </summary>
    OpenGameResponse,

    /// <summary>
    /// 暂停游戏请求
    /// <para>[无]</para>
    /// </summary>
    PauseGameRequest,

    /// <summary>
    /// 暂停游戏响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    PauseGameResponse,

    /// <summary>
    /// 恢复游戏请求
    /// <para>[无]</para>
    /// </summary>
    ResumeGameRequest,

    /// <summary>
    /// 恢复游戏响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    ResumeGameResponse,

    /// <summary>
    /// 关闭游戏请求
    /// <para>[无]</para>
    /// </summary>
    StopGameRequest,

    /// <summary>
    /// 关闭游戏响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    StopGameResponse,

    /// <summary>
    /// 图像数据请求
    /// <para>[(画面序号(<see langword="uint"/>), <see langword="DeflateStream"/>压缩后的画面数据(<see langword="byte[]"/>))]</para>
    /// </summary>
    ImageDataRequest,

    /// <summary>
    /// 图像数据响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    ImageDataResponse,

    /// <summary>
    /// 音频数据请求
    /// <para>[音频数据(<see langword="float[128]"/>))]</para>
    /// </summary>
    AudioDataRequest,

    /// <summary>
    /// 音频数据响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    AudioDataResponse,

    /// <summary>
    /// 操作数据
    /// <para>[操作数据(<see langword="byte[8]"/>)]</para>
    /// </summary>
    OperationSyncRequest,

    /// <summary>
    /// 操作数据响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    OperationSyncResponse,

    /// <summary>
    /// 文件信息请求
    /// <para>[(文件数量(<see langword="int"/>), 用<see langword="BinaryWriter"/>(Mapper,文件名)(<see langword="(int,string)[]"/>))]</para>
    /// </summary>
    NesFileInfosRequest,

    /// <summary>
    /// 文件信息响应
    /// <para>[是否成功(<see langword="bool"/>)]</para>
    /// </summary>
    NesFileInfosResponse,

    /// <summary>
    /// 心跳请求
    /// <para>[无]</para>
    /// </summary>
    HeartbeatRequest,

    /// <summary>
    /// 心跳响应
    /// <para>[无]</para>
    /// </summary>
    HeartbeatResponse,

    /// <summary>
    /// 无效数据帧
    /// </summary>
    None = 0xFF,
}

