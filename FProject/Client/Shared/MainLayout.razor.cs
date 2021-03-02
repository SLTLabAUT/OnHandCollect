using BlazorFluentUI;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Client.Shared
{
    public partial class MainLayout
    {
        private bool isPanelOpen = false;

        [CascadingParameter]
        public ResponsiveMode CurrentMode { get; set; }

        void ShowMenu()
        {
            isPanelOpen = true;
        }

        void HideMenu()
        {
            isPanelOpen = false;
        }

        void OnNavLinkClick(BFUNavLink linkBase)
        {
            HideMenu();
        }
    }
}
