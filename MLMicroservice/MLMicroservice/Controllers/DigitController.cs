using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using MLMicroservice.MLModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MLMicroservice.Controllers
{
    [Route("api/v1/predictions")]
    [ApiController]
    public class DigitController : ControllerBase
    {
        private const int SizeOfImage = 32;
        private const int SizeOfArea = 4;

        private readonly PredictionEnginePool<DigitData, DigitPrediction> _predictionEnginePool;

        public DigitController(PredictionEnginePool<DigitData,DigitPrediction> predictionEnginePool)
        {
            this._predictionEnginePool = predictionEnginePool;
        }
        private static List<float> GetPixelValuesFromImage(string base64Image)
        {
            var imageBytes = Convert.FromBase64String(base64Image).ToArray();

  
            var bitmap = new Bitmap(SizeOfImage, SizeOfImage);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                using var stream = new MemoryStream(imageBytes);
                var png = Image.FromStream(stream);
                g.DrawImage(png, 0, 0, SizeOfImage, SizeOfImage);
            }

     

            var result = new List<float>();
            for (var i = 0; i < SizeOfImage; i += SizeOfArea)
            {
                for (var j = 0; j < SizeOfImage; j += SizeOfArea)
                {
                    var sum = 0;        
                    for (var k = i; k < i + SizeOfArea; k++)
                    {
                        for (var l = j; l < j + SizeOfArea; l++)
                        {
                            if (bitmap.GetPixel(l, k).Name != "ffffffff") sum++;
                        }
                    }
                    result.Add(sum);
                }
            }

            return result;
        }
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<string> Post([FromBody] string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                return BadRequest(new { prediction = "-", dataset = string.Empty });
            }
            var pixelValues = GetPixelValuesFromImage(base64Image);
            var input = new DigitData { PixelValues = pixelValues.ToArray() };
            Console.WriteLine(input);
            var result = _predictionEnginePool.Predict(modelName: "DigitAnalysisModel", example: input);
            return Ok(new
            {
                prediction = result.Prediction,
                scores = result.Score,
                pixelValues = string.Join(",", pixelValues)
            });
        }

     
    }
}
