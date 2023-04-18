using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModTime.Library
{
    public class UIControlManager : MonoBehaviour
    {

        public static float LeftLabeledValueHorizontalSlider(Rect screenRect, float sliderValue, float sliderMinValue, float sliderMaxValue, string labelText)
        {
            GUI.Label(screenRect, $"{labelText} ({Mathf.Round(sliderValue)})");

            // <- Push the Slider to the end of the Label
            screenRect.x += screenRect.width;

            sliderValue = GUI.HorizontalSlider(screenRect, sliderValue, sliderMinValue, sliderMaxValue);
            return sliderValue;
        }

    }
}
