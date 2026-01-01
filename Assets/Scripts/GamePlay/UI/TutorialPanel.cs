using System;
using UI.Base;
using UnityEngine;
using Utils;

namespace UI
{
    public class TutorialPanel : UIPanel
    {
        private void OnEnable()
        {
            if (ClientPrefs.GetBool(ClientPrefs.READED_TUTORIAL_KEY))
            {
                Hide();
            }
            else
            {
                this.gameObject.SetActive(true);
                Time.timeScale = 0f;
            }
        }
        
        public void Hide()
        {
            this.gameObject.SetActive(false);
            ClientPrefs.SetBool(ClientPrefs.READED_TUTORIAL_KEY, true);
            Time.timeScale = 1f;
        }
    }
}