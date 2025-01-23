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

namespace NesEmu.Core.Mappers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MapperAttribute : Attribute
    {
        #region Public Constructors

        public MapperAttribute(int number, string name)
        {
            Number = number;
            Name = name;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Name { get; }
        public int Number { get; }

        #endregion Public Properties
    }
}