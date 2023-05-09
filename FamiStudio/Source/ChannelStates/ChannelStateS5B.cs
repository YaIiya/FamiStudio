﻿using System;

namespace FamiStudio
{
    public class ChannelStateS5B : ChannelState
    {
        int channelIdx = 0;
        int toneReg = 0x38;
        int mask = 0xff;

        public ChannelStateS5B(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.S5BSquare1;
            mask = mask - (9 << channelIdx);
        }

        public override void YMMixerSettingsChangedNotify(int  ymMixerSettings)
        {
            toneReg = ymMixerSettings;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;
                var noiseFreq = envelopeValues[EnvelopeType.YMNoiseFreq];
                player.NotifyYMMixerSettingsChanged(
                    ((toneReg & mask) | ((((envelopeValues[EnvelopeType.YMMixerSettings] & 0x1) | ((envelopeValues[EnvelopeType.YMMixerSettings] & 0x2) << 2))) << channelIdx)),
                    (1L << ChannelType.S5BSquare1) |
                    (1L << ChannelType.S5BSquare2) |
                    (1L << ChannelType.S5BSquare3));
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_MIXER_SETTING);
                WriteRegister(NesApu.S5B_DATA, toneReg);
                if (noiseFreq > 0)
                {
                    WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_NOISE_FREQ);
                    WriteRegister(NesApu.S5B_DATA, noiseFreq);
                }
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodLo);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodHi);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, volume);
            }

            // HACK : There are conflicts between N163 registers and S5B register, a N163 addr write
            // can be interpreted as a S5B data write. To prevent this, we select a dummy register 
            // for S5B so that these writes will be discarded.
            //
            // N163: 
            //   f800-ffff (addr)
            //   4800-4fff (data)
            // S5B:
            //   c000-e000 (addr)
            //   f000-ffff (data)

            if ((NesApu.GetAudioExpansions(apuIdx) & NesApu.APU_EXPANSION_MASK_NAMCO) != 0)
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_IO_A);

            base.UpdateAPU();
        }
    };
}
