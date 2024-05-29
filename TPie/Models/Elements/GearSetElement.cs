using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Numerics;
using TPie.Helpers;

namespace TPie.Models.Elements
{
    public class GearSetElement : RingElement
    {
        public uint GearSetID;
        public bool UseID;
        public string GearSetName;
        public bool DrawText;
        public bool DrawTextOnlyWhenSelected;
        public string AdditionalCommand1;
        public string AdditionalCommand2;
        public string AdditionalCommand3;

        private uint _jobId;

        [JsonProperty]
        public uint JobID
        {
            get => _jobId;
            set
            {
                _jobId = value;
                IconID = 62800 + value;
            }
        }

        public GearSetElement(uint gearSetId, bool useId, string? name, bool drawText, bool drawTextOnlyWhenSelected, uint jobId, string[]? additionalCommands)
        {
            GearSetID = gearSetId; 
            UseID = useId;
            GearSetName = name ?? "";
            DrawText = drawText;
            DrawTextOnlyWhenSelected = drawTextOnlyWhenSelected;
            JobID = jobId;
            AdditionalCommand1 = !String.IsNullOrEmpty(additionalCommands?[0]) ? additionalCommands[0] : "";
            AdditionalCommand2 = !String.IsNullOrEmpty(additionalCommands?[1]) ? additionalCommands[1] : "";
            AdditionalCommand3 = !String.IsNullOrEmpty(additionalCommands?[2]) ? additionalCommands[2] : "";
        }

        public GearSetElement() : this(1, true, null, true, false, Plugin.ClientState.LocalPlayer?.ClassJob.Id ?? JobIDs.GLA, null) { }

        public override void ExecuteAction()
        {
            string[] addCommands = [AdditionalCommand1, AdditionalCommand2, AdditionalCommand3];

            if (UseID)
            {
                ChatHelper.SendChatMessage($"/gs change {GearSetID}");
                
                foreach (var command in addCommands)
                {

                    if (!String.IsNullOrEmpty(command))
                    {
                        ChatHelper.SendChatMessage(command);
                    }
                }
            }
            else
            {
                ChatHelper.SendChatMessage($"/gs change \"{GearSetName}\"");
                
                foreach (var command in addCommands)
                {

                    if (!String.IsNullOrEmpty(command))
                    {
                        ChatHelper.SendChatMessage(command);
                    }
                }
            }
        }

        public override bool IsValid()
        {
            return (string.IsNullOrEmpty(AdditionalCommand1) || AdditionalCommand1.StartsWith('/'))
                && (string.IsNullOrEmpty(AdditionalCommand2) || AdditionalCommand2.StartsWith('/'))
                && (string.IsNullOrEmpty(AdditionalCommand3) || AdditionalCommand3.StartsWith('/'));
        }

        public override string InvalidReason()
        {
            return "Additional Command format is invalid";
        }

        public override string Description()
        {
            if (JobsHelper.JobNames.TryGetValue(JobID, out string? value) && value != null)
            {
                if (UseID)
                {
                    return $"{value} ({GearSetID})";
                }

                return value == GearSetName ? value : $"{value} ({GearSetName})";
            }
            
            return "";
        }


        public override void Draw(Vector2 position, Vector2 size, float scale, bool selected, uint color, float alpha, bool tooltip, ImDrawListPtr drawList)
        {
            base.Draw(position, size, scale, selected, color, alpha, tooltip, drawList);

            if (!DrawText) { return; }

            if (!DrawTextOnlyWhenSelected || (DrawTextOnlyWhenSelected && selected))
            {
                size = size * scale;
                string text = UseID ? $"{GearSetID}" : $"{GearSetName}";
                Vector2 textPos = UseID ? position + (size / 2f) - new Vector2(2 * scale) : position;
                DrawHelper.DrawOutlinedText(text, textPos, true, scale, drawList);
            }
        }
    }
}
