using FProject.Client.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FProject.Client.Pages
{
    public partial class EmailSender
    {
        [Inject]
        HttpClient Http { get; set; }

        bool Done { get; set; }
        EmailSenderModel Model { get; set; }
        EditContext EditContext { get; set; }
        Button SubmitButton { get; set; }

        protected override void OnInitialized()
        {
            //Http.Timeout = Timeout.InfiniteTimeSpan;
        }

        protected override void OnParametersSet()
        {
            Model = new EmailSenderModel();
            EditContext = new EditContext(Model);
        }

        async Task FormHandler()
        {
            SubmitButton.State = ButtonState.Acting;
            try
            {
                Done = false;

                var result = await Http.PostAsJsonAsync($"api/Email/SendEmail", (EmailSenderDTO)Model);
                result.EnsureSuccessStatusCode();

                Done = true;
            }
            finally
            {
                SubmitButton.State = ButtonState.None;
            }
        }

        void DismissMessagebarHandler()
        {
            Done = false;
        }

        public class EmailSenderModel
        {
            [Required]
            [Display(Name = "موضوع/عنوان")]
            public string Subject { get; set; }
            [Required]
            [Display(Name = "توضیحات")]
            public string Description { get; set; }
            [Required]
            [Display(Name = "توضیحات ساده")]
            public string TextDescription { get; set; }
            [Required]
            [Display(Name = "گیرنده‌ها")]
            public string Tos { get; set; }

            public static explicit operator EmailSenderDTO(EmailSenderModel model)
            {
                return new EmailSenderDTO
                {
                    Subject = model.Subject,
                    Description = model.Description,
                    TextDescription = model.TextDescription,
                    Tos = model.Tos.Replace(" ", "").Split(",")
                };
            }
        }
    }
}
