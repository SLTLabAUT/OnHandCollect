using FProject.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class CommentSection
    {
        [Parameter]
        public IEnumerable<CommentDTO> Comments { get; set; }
    }
}
