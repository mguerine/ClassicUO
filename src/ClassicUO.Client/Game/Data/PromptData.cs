// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal enum ConsolePrompt
    {
        None,
        ASCII,
        Unicode
    }

    internal readonly struct PromptData
    {
        public readonly ConsolePrompt Prompt;
        public readonly ulong Data;

        public PromptData(ConsolePrompt prompt, ulong data)
        {
            Prompt = prompt;
            Data = data;
        }
    }
}