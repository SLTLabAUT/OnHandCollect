﻿using FProject.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class Writepad
    {
        public int Id { get; set; }

        public WritepadDataType Type { get; set; }
        public PointerType PointerType { get; set; }
        public string Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }

        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }
    }
}
