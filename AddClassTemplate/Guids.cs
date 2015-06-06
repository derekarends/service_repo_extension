// Guids.cs
// MUST match guids.h
using System;

namespace ThinkovatorInc.AddClassTemplate
{
    static class GuidList
    {
        public const string guidAddClassTemplatePkgString = "418a0b4d-1aab-4436-a2f1-30d7c5be76d8";
        public const string guidAddClassTemplateCmdSetString = "88a88f0e-c4e5-4a0d-88a5-295a6dbb7a57";

        public static readonly Guid guidAddClassTemplateCmdSet = new Guid(guidAddClassTemplateCmdSetString);
    };
}