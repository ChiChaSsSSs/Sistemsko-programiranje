using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat3.SentimentAnalysis
{
    public class SentimentData
    {
        [LoadColumn(0)]
        public bool Label { get; set; }

        [LoadColumn(2)]
        public string Text { get; set; } = null!;
    }
}
