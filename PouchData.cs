﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolPouch
{
    public class PouchData
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Texture { get; set; } = "CodeWordZ.ToolPouch/pouch.png";
        public int TextureIndex { get; set; } = 0;

        public int Capacity { get; set; }
    }
}
