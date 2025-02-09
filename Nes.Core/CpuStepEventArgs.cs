// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2023
// MIT License
// ============================================================================

using System;

namespace Nes.Core
{
    /// <summary>
    /// Represents the class that carries the CPU stepping or CPU stepped event data.
    /// </summary>
    public sealed class CpuStepEventArgs : EventArgs
    {

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <c>CpuStepEventArgs</c> class.
        /// </summary>
        /// <param name="cpu">The instance of the CPU executing the instruction.</param>
        /// <param name="instruction">The instruction that the CPU is currently executing.</param>
        /// <param name="resolvedAddress">The resolved address where the instruction would operate at.</param>
        /// <param name="currentCycles">Current CPU cycles.</param>
        public CpuStepEventArgs(Cpu cpu, byte[] instruction, ushort resolvedAddress, long currentCycles)
        {
            Cpu = cpu;
            A = cpu.A;
            Flags = cpu.Flag;
            AddressingMode = Cpu.GetInstructionAddressingMode(instruction[0]);
            Instruction = instruction;
            OpcodeName = cpu.GetInstructionName(instruction[0]);
            PC = cpu.PC;
            ResolvedAddress = resolvedAddress;
            SP = cpu.SP;
            X = cpu.X;
            Y = cpu.Y;
            CurrentCycles = currentCycles;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets the value of the accumulator.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets the addressing mode of the current instruction.
        /// </summary>
        public AddressingMode AddressingMode { get; }

        /// <summary>
        /// Gets the total number of cycles that the CPU has executed.
        /// </summary>
        public long CurrentCycles { get; }

        /// <summary>
        /// Gets the disassembled string representation of the current instruction.
        /// </summary>
        public string DisassembledInstruction => Cpu.Disassemble(PC, Instruction);

        /// <summary>
        /// Gets the CPU status flags.
        /// </summary>
        public CpuFlags Flags { get; }

        /// <summary>
        /// Gets the current instruction.
        /// </summary>
        public byte[] Instruction { get; }

        /// <summary>
        /// Gets a <see cref="bool"/> value which indicates whether the current instruction is a BRK instruction.
        /// </summary>
        public bool IsBreakInstruction => Instruction is not null && Instruction.Length == 1 && Instruction[0] == 0;

        /// <summary>
        /// Gets the name of the current Opcode.
        /// </summary>
        public string OpcodeName { get; }

        /// <summary>
        /// Gets the current program counter.
        /// </summary>
        public ushort PC { get; }

        /// <summary>
        /// Gets the resolved address where the instruction would operate at.
        /// </summary>
        public ushort ResolvedAddress { get; }

        /// <summary>
        /// Gets the current stack pointer.
        /// </summary>
        public byte SP { get; }

        /// <summary>
        /// Gets the value of the X register.
        /// </summary>
        public byte X { get; }

        /// <summary>
        /// Gets the value of the Y register.
        /// </summary>
        public byte Y { get; }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// Gets the instance of the CPU.
        /// </summary>
        internal Cpu Cpu { get; }

        #endregion Internal Properties

        #region Public Methods

        /// <inheritdoc/>
        public override string ToString( ) => $"A=${A.ToString("X").PadLeft(2, '0')} X=${X.ToString("X").PadLeft(2, '0')} Y=${Y.ToString("X").PadLeft(2, '0')} SP=${SP.ToString("X").PadLeft(2, '0')} PC=${PC.ToString("X").PadLeft(4, '0')} P=${Flags}";

        #endregion Public Methods

    }
}