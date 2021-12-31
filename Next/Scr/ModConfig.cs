﻿using System.Collections.Generic;
using System.Reflection;

namespace SkySwordKill.Next
{
    public class ModConfig
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public bool Success { get; set; }
        public string Path { get; set; }

        public Dictionary<string, string> jsonPathCache = new Dictionary<string, string>();
    }
}