using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Kimi.Logging;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kimi.Services.Commands.Milkshake
{
    public class ModalEvents
    {
        public event DataReceived? OnDataReceived;
        public delegate Task DataReceived(MilkshakeDataReceived data, (string, string?) modal, ulong id);

        public event EventHandler? OnSent;

        public async Task OnMilkshakeSent(object module, OnSentArgs args)
        {
            OnSent?.Invoke(module, args);
            await Task.CompletedTask;
        }

        public Task? ModalDataReceived(IAttachment attachment, (string, string?) modal, ulong id)
        {
            return OnDataReceived?.Invoke(new MilkshakeDataReceived(attachment), modal, id);
        }

        public event ModalHandled? OnModalSent;
        public delegate Task ModalHandled(ulong id, (string, string?) modal);

        public async Task ModalSent(ulong id, (string, string?) modal)
        {
            if (OnModalSent is not null)
                await OnModalSent.Invoke(id, modal);
        }
    }

    public class OnSentArgs : EventArgs
    {
        public string CustomId { get; set; } = string.Empty;
        public IUser User { get; set; } = null!;
        public bool IsFinished { get; set; } = false;
    }

    public class MilkshakeDataReceived
    {
        //public string FileName { get; set; }
        //public string Type { get; set; }

        //public int Width { get; set; }
        //public int Height { get; set; }

        //public MilkshakeDataReceived(string fileName, string type, int width, int height)
        //{
        //    FileName = fileName;
        //    Type = type;
        //    Width = width;
        //    Height = height;
        //}

        public IAttachment Attachment { get; set; }

        public MilkshakeDataReceived(IAttachment attachment)
        {
            Attachment = attachment;
        }
    }
}
