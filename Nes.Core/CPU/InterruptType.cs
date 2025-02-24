namespace Nes.Core;

internal enum InterruptType
{
    Brk,    // 软件中断
    Irq,    // 可屏蔽中断
    Nmi,    // 不可屏蔽中断
}
