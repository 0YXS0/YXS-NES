using NAudio.Wave;

namespace Nes.Widget.Models;

/// <summary>
/// 音频提供器
/// </summary>
internal class WriteLine : WaveProvider32
{
    private readonly float[] cyclicBuffer = [];
    private int readIndex;
    private int writeIndex;
    private int size;
    private readonly object queueLock = new( );

    public bool Enabled { get; set; }

    public WriteLine( )
    {
        cyclicBuffer = new float[4096];
        readIndex = writeIndex = 0;
        Enabled = true;
    }

    public override int Read(float[] buffer, int offset, int sampleCount)
    {
        lock(queueLock)
        {
            if(!Enabled || size == 0)
            {
                buffer[offset] = 0;
                return 1;
            }

            sampleCount = Math.Min(sampleCount, size);

            for(int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = cyclicBuffer[readIndex++];
                readIndex %= cyclicBuffer.Length;
                --size;
            }
            return sampleCount;
        }
    }

    public void Queue(float[] sampleValues)
    {
        lock(queueLock)
        {
            for(int index = 0; index < sampleValues.Length; index++)
            {
                if(size >= cyclicBuffer.Length)
                    return;

                cyclicBuffer[writeIndex] = sampleValues[index];
                ++writeIndex;
                writeIndex %= cyclicBuffer.Length;
                ++size;
            }
        }
    }
}

