
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Otm.Mvc.ViewModel
{
    public class DeleteViewModel<T>
    {
        public DeleteViewModel()
        {

        }

        public DeleteViewModel(T id, string text, bool confirm = false)
        {
            Id = id;
            Text = text;
            Confirm = confirm;
        }

        public T Id { get; set; }
        public String Text { get; set; }
        public bool? Confirm { get; set; }
    }
}