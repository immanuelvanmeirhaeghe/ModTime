using ModTime.Data.Player.Condition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModTime.Managers
{
    public class UIControlManager : MonoBehaviour
    {
        private static Color DefaultColor = GUI.color;
        private static Color DefaultContentColor = GUI.contentColor;
        private static Color DefaultBackGroundColor = GUI.backgroundColor;

        public static float CustomHorizontalSlider(float sliderValue, float sliderMinValue, float sliderMaxValue, string labelText)
        {
            GUI.contentColor = DefaultContentColor;            
            if (labelText.ToLower().Contains("carbo"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Carbo);
            }
            if (labelText.ToLower().Contains("fat"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Fat);
            }
            if (labelText.ToLower().Contains("proteins"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Proteins);
            }
            if (labelText.ToLower().Contains("oxygen") || labelText.ToLower().Contains("hydration"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Hydration);
            }
            if (labelText.ToLower().Contains("energy") || labelText.ToLower().Contains("stamina") || labelText.ToLower().Contains("health"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Energy);
            }

            using (var sliderHScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label($"{labelText} ({Mathf.Round(sliderValue)})");
                sliderValue = GUILayout.HorizontalSlider(sliderValue, sliderMinValue, sliderMaxValue);
                return sliderValue;
            }

        }

    }
}
