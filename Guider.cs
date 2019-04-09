﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SkorBlazor.GuidePopup
{
    public class Guider : IGuider
    {
        private Queue<GuideStep> GuideLines { get; set; }
        private GuiderSetting Setting;
        private string Id { get; } = Guid.NewGuid().ToString();
        private readonly IJSRuntime _jSRuntime;
        public Guider(IJSRuntime jSRuntime)
        {
            GuideLines = new Queue<GuideStep>();
            Setting = new GuiderSetting();
            _jSRuntime = jSRuntime;
        }
        public Guider(IJSRuntime jSRuntime, Action<GuiderSetting> options) : this(jSRuntime)
        {
            options(Setting);
        }
        public event EventHandler OnClosed;

        public Task Show(ElementRef element, string content, GuidePosition guidePosition = GuidePosition.Right)
        {
            return _jSRuntime.InvokeAsync<object>("guiderJsFunctions.showWithElementRef", Setting, Id, element, content, guidePosition, new DotNetObjectRef(this));
        }

        public Task Show(string elementId, string content, GuidePosition guidePosition = GuidePosition.Right)
        {
            return _jSRuntime.InvokeAsync<object>("guiderJsFunctions.showWithElementId", Setting, Id, elementId, content, guidePosition, new DotNetObjectRef(this));
        }

        public Task Show(double x, double y, string content, GuidePosition guidePosition = GuidePosition.Right)
        {
            return _jSRuntime.InvokeAsync<object>("guiderJsFunctions.showWithXY", Setting, Id, x, y, content, guidePosition, new DotNetObjectRef(this));
        }
        [JSInvokable]
        public void InvokeClosed()
        {
            OnClosed?.Invoke(this, null);
        }

        public IGuider Make(GuideStep guideStep)
        {
            GuideLines.Enqueue(guideStep);
            return this;
        }

        public async Task Start()
        {
            while (GuideLines.Count != 0)
            {
                bool closed = false;
                GuideStep step = GuideLines.Dequeue();
                this.OnClosed += (s, e) =>
                {
                    closed = true;
                };
                await ShowStep(step);
                while (!closed)
                    await Task.Delay(100);
            }
        }

        private Task ShowStep(GuideStep guideStep)
        {
            if (guideStep.GuideType == GuideType.Id)
                return Show(guideStep.ElementId, guideStep.Content, guideStep.GuidePosition);
            if (guideStep.GuideType == GuideType.Ref)
                return Show(guideStep.ElementRef, guideStep.Content, guideStep.GuidePosition);
            return Show(guideStep.X, guideStep.Y, guideStep.Content, guideStep.GuidePosition);
        }
    }
    public class GuideStep
    {
        private GuideStep(string content, GuidePosition guidePosition)
        {
            this.Content = content;
            this.GuidePosition = guidePosition;
        }
        public GuideStep(string elementId, string content, GuidePosition guidePosition = GuidePosition.Right) : this(content, guidePosition)
        {
            this.ElementId = elementId;
            this.GuideType = GuideType.Id;
        }
        public GuideStep(ElementRef elementRef, string content, GuidePosition guidePosition = GuidePosition.Right) : this(content, guidePosition)
        {
            this.ElementRef = elementRef;
            this.GuideType = GuideType.Ref;
        }
        public GuideStep(double x, double y, string content, GuidePosition guidePosition = GuidePosition.Right) : this(content, guidePosition)
        {
            this.X = x;
            this.Y = y;
            this.GuideType = GuideType.Coordination;
        }
        public GuideType GuideType { get; set; }
        public string Content { get; set; }
        public GuidePosition GuidePosition { get; set; }
        public string ElementId { get; set; }
        public ElementRef ElementRef { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
    public enum GuideType
    {
        Ref,
        Id,
        Coordination
    }
}
