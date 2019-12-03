/**
 * 登录场景UI
 * **/
using ETModel;
using FairyGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ETHotfix
{
    public class LoginSceneUI_FairyGUI : BaseUIForms
    {
        //登录按钮
        public const string BTN_LOGIN = "n15";
        //版本文本
        public const string TEXT_VERTION = "Ver";

        string openId = "test233";
        string userName = "超级玛丽X";
        string url = "https://t12.baidu.com/it/u=3054907793,3255690286&fm=173&app=25&f=JPEG?w=327&h=336&s=F2C4F001463B1B9E35046DB203008080";

        private GTextField gAccount = null;
        private GTextField gName = null;
        private GTextField gUrl = null;

        public LoginSceneUI_FairyGUI()
        {
            this.pakName = "Login";
            this.cmpName = "loginBg";
            this.CurrentUIType.NeedClearingStack = true;
            this.CurrentUIType.UIForms_ShowMode = UIFormsShowMode.HideOther;
            this.CurrentUIType.UIForms_Type = UIFormsType.Normal;

        }

        public override void InitUI()
        {
            base.InitUI();
            GComponent gcmp;
            gcmp = this.GObject.asCom;
            gcmp.GetChild(BTN_LOGIN).asButton.onClick.Add(OneBtnClick);
            //检查版本/更新操作。。
            gcmp.GetChild(TEXT_VERTION).asTextField.text = "2016.0808V_T";


            //因为是登陆页面，重置sessiond等。
            Game.Scene.RemoveComponent<SessionComponent>();
            ETModel.Game.Scene.RemoveComponent<ETModel.SessionComponent>();
            //设置三个输入框

            this.gAccount = gcmp.GetChild("account").asCom.GetChild("input_text").asTextField;
            this.gName = gcmp.GetChild("name").asCom.GetChild("input_text").asTextField;
            this.gUrl = gcmp.GetChild("url").asCom.GetChild("input_text").asTextField;

            this.gAccount.text = "test233";
            this.gName.text = "超级玛丽X";
            this.gUrl.text = "https://t12.baidu.com/it/u=3054907793,3255690286&fm=173&app=25&f=JPEG?w=327&h=336&s=F2C4F001463B1B9E35046DB203008080";

            //SoundComponent.Instance?.PlayMusic(SoundName.loginBgm, 1, 1, true, false);

        }

        public override void DoShowAnimationEvent()
        {

        }

        //按钮点击事件
        private void OneBtnClick()
        {
            string openId = this.gAccount.text;
            string userName = this.gName.text;
            string url = this.gUrl.text;

            //Game.EventSystem.Run(EventIdType.LoginEvent, openId, userName, url);
            //   ETModel.Game.Scene.GetComponent<ShareSdkComponent>().Authorize();

        }

        public override Task HideEvent()
        {
            //SoundComponent.Instance?.Stop(SoundName.loginBgm);
            return base.HideEvent();
        }
    }//class_end
}
