//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using Emul8.Core;
using Emul8.Peripherals.Bus;
using Emul8.Logging;

namespace Emul8.Peripherals.Timers
{
    public sealed class CC2538SleepTimer : ComparingTimer, IDoubleWordPeripheral, IKnownSize
    {
        public CC2538SleepTimer(Machine machine) : base(machine, 32768, uint.MaxValue, uint.MaxValue)
        {
            IRQ = new GPIO();
            Enabled = true;
        }

        public uint ReadDoubleWord(long offset)
        {
            switch((Registers)offset)
            {
            case Registers.CountAndCompare0:
                lastValue = (uint)Value;
                return (uint)(byte)lastValue;
            case Registers.CountAndCompare1:
                return (uint)(byte)(lastValue >> 8);
            case Registers.CountAndCompare2:
                return (uint)(byte)(lastValue >> 16);
            case Registers.CountAndCompare3:
                return (uint)(byte)(lastValue >> 24);
            case Registers.LoadStatus:
                return 1u; // we're always ready and have the value already loaded
            default:
                this.LogUnhandledRead(offset);
                return 0;
            }
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            switch((Registers)offset)
            {
            case Registers.CountAndCompare0:
                lastCompare &= 0xFFFFFF00;
                lastCompare |= value;
                Compare = lastCompare;
                break;
            case Registers.CountAndCompare1:
                lastCompare &= 0xFFFF00FF;
                lastCompare |= (value << 8);
                break;
            case Registers.CountAndCompare2:
                lastCompare &= 0xFF00FFFF;
                lastCompare |= (value << 16);
                break;
            case Registers.CountAndCompare3:
                lastCompare &= 0x00FFFFFF;
                lastCompare |= (value << 24);
                break;
            default:
                this.LogUnhandledWrite(offset, value);
                break;
            }
        }

        public override void Reset()
        {
            base.Reset();
            lastValue = 0;
            lastCompare = 0;
        }

        public long Size
        {
            get
            {
                return 0x30;
            }
        }

        public GPIO IRQ { get; private set; }

        protected override void OnCompare()
        {
            IRQ.Set();
            IRQ.Unset();
        }

        private uint lastValue;
        private uint lastCompare;

        private enum Registers
        {
            CountAndCompare0 = 0x00,
            CountAndCompare1 = 0x04,
            CountAndCompare2 = 0x08,
            CountAndCompare3 = 0x0C,
            LoadStatus       = 0x10,
            CaptureControl   = 0x14,
            CaptureStatus    = 0x18,
            CaptureValue0    = 0x1C,
            CaptureValue1    = 0x20,
            CaptureValue2    = 0x24,
            CaptureValue3    = 0x28
        }
    }
}
