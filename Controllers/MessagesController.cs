using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using PortChatBot.DB;
using PortChatBot.Models;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Web.Configuration;
using PortChatBot.Dialogs;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.ConnectorEx;

namespace PortChatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        public static readonly string TEXTDLG = "2";
        public static readonly string CARDDLG = "3";
        public static readonly string MEDIADLG = "4";
        public static readonly int MAXFACEBOOKCARDS = 10;

        public static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string chatBotAppID = "appID";
        public static int appID = Convert.ToInt32(rootWebConfig.ConnectionStrings.ConnectionStrings[chatBotAppID].ToString());

        //config 변수 선언
        static public string[] LUIS_NM = new string[10];        //루이스 이름
        static public string[] LUIS_APP_ID = new string[10];    //루이스 app_id
        static public string LUIS_SUBSCRIPTION = "";            //루이스 구독키
        static public int LUIS_TIME_LIMIT;                      //루이스 타임 체크
        static public string QUOTE = "";                        //견적 url
        static public string TESTDRIVE = "";                    //시승 url
        static public string BOT_ID = "";                       //bot id
        static public string MicrosoftAppId = "";               //app id
        static public string MicrosoftAppPassword = "";         //app password
        static public string LUIS_SCORE_LIMIT = "";             //루이스 점수 체크

        public static int sorryMessageCnt = 0;
        public static int chatBotID = 0;

        public static int pagePerCardCnt = 10;
        public static int pageRotationCnt = 0;
        public static int fbLeftCardCnt = 0;
        public static int facebookpagecount = 0;
        public static string FB_BEFORE_MENT = "";

        public static List<AnalysisList> analysisList = new List<AnalysisList>();
        public static List<TrendList> trendList = new List<TrendList>();
        public static List<HrList> hrList = new List<HrList>();
        public static List<WeatherList> weatherList = new List<WeatherList>();
        public static List<RelationList> relationList = new List<RelationList>();
        public static string luisId = "";
        public static string luisIntent = "";
        public static string luisEntities = "";
        public static string queryStr = "";
        public static DateTime startTime;

        public static CacheList cacheList = new CacheList();
        //페이스북 페이지용
        public static ConversationHistory conversationhistory = new ConversationHistory();
        //추천 컨텍스트 분석용
        public static Dictionary<String, String> recommenddic = new Dictionary<string, String>();
        //결과 플레그 H : 정상 답변, S : 기사검색 답변, D : 답변 실패
        public static String replyresult = "";
        //API 플레그 QUOT : 견적, TESTDRIVE : 시승 RECOMMEND : 추천 COMMON : 일반 SEARCH : 검색
        public static String apiFlag = "";
        public static String recommendResult = "";

        public static string channelID = "";

        public static DbConnect db = new DbConnect();
        public static DButil dbutil = new DButil();

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            string cashOrgMent = "";

            //DbConnect db = new DbConnect();
            //DButil dbutil = new DButil();
            DButil.HistoryLog("db connect !! " );
            //HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            HttpResponseMessage response ;

            Activity reply1 = activity.CreateReply();
            Activity reply2 = activity.CreateReply();
            Activity reply3 = activity.CreateReply();
            Activity reply4 = activity.CreateReply();

            // Activity 값 유무 확인하는 익명 메소드
            Action<Activity> SetActivity = (act) =>
            {
                if (!(reply1.Attachments.Count != 0 || reply1.Text != ""))
                {
                    reply1 = act;
                }
                else if (!(reply2.Attachments.Count != 0 || reply2.Text != ""))
                {
                    reply2 = act;
                }
                else if (!(reply3.Attachments.Count != 0 || reply3.Text != ""))
                {
                    reply3 = act;
                }
                else if (!(reply4.Attachments.Count != 0 || reply4.Text != ""))
                {
                    reply4 = act;
                }
                else
                {

                }
            };

            //DButil.HistoryLog("activity.Recipient.Name : " + activity.Recipient.Name);
            //DButil.HistoryLog("activity.Name : " + activity.Name);

            if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                startTime = DateTime.Now;
                //activity.ChannelId = "facebook";
                //파라메터 호출
                if (LUIS_NM.Count(s => s != null) > 0)
                {
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    //string[] LUIS_APP_ID = new string[10];
                    Array.Clear(LUIS_APP_ID, 0, LUIS_APP_ID.Length);
                }
                //Array.Clear(LUIS_APP_ID, 0, 10);
                DButil.HistoryLog("db SelectConfig start !! ");
                List<ConfList> confList = db.SelectConfig();
                DButil.HistoryLog("db SelectConfig end!! ");

                for (int i = 0; i < confList.Count; i++)
                {
                    switch (confList[i].cnfType)
                    {
                        case "LUIS_APP_ID":
                            LUIS_APP_ID[LUIS_APP_ID.Count(s => s != null)] = confList[i].cnfValue;
                            LUIS_NM[LUIS_NM.Count(s => s != null)] = confList[i].cnfNm;
                            break;
                        case "LUIS_SUBSCRIPTION":
                            LUIS_SUBSCRIPTION = confList[i].cnfValue;
                            break;
                        case "BOT_ID":
                            BOT_ID = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppId":
                            MicrosoftAppId = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppPassword":
                            MicrosoftAppPassword = confList[i].cnfValue;
                            break;
                        case "QUOTE":
                            QUOTE = confList[i].cnfValue;
                            break;
                        case "TESTDRIVE":
                            TESTDRIVE = confList[i].cnfValue;
                            break;
                        case "LUIS_SCORE_LIMIT":
                            LUIS_SCORE_LIMIT = confList[i].cnfValue;
                            break;
                        case "LUIS_TIME_LIMIT":
                            LUIS_TIME_LIMIT = Convert.ToInt32(confList[i].cnfValue);
                            break;
                        default: //미 정의 레코드
                            Debug.WriteLine("* conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            DButil.HistoryLog("* conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            break;
                    }
                }

                Debug.WriteLine("* DB conn : " + activity.Type);
                DButil.HistoryLog("* DB conn : " + activity.Type);

                //초기 다이얼로그 호출
                List<DialogList> dlg = db.SelectInitDialog(activity.ChannelId);

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                foreach (DialogList dialogs in dlg)
                {
                    Activity initReply = activity.CreateReply();
                    initReply.Recipient = activity.From;
                    initReply.Type = "message";
                    initReply.Attachments = new List<Attachment>();
                    //initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    Attachment tempAttachment;

                    if (dialogs.dlgType.Equals(CARDDLG))
                    {
                        foreach (CardList tempcard in dialogs.dialogCard)
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                            initReply.Attachments.Add(tempAttachment);
                        }
                    }
                    else
                    {
                        if (activity.ChannelId.Equals("facebook") && string.IsNullOrEmpty(dialogs.cardTitle) && dialogs.dlgType.Equals(TEXTDLG))
                        {
                            Activity reply_facebook = activity.CreateReply();
                            reply_facebook.Recipient = activity.From;
                            reply_facebook.Type = "message";
                            DButil.HistoryLog("facebook  card Text : " + dialogs.cardText);
                            reply_facebook.Text = dialogs.cardText;
                            var reply_ment_facebook = connector.Conversations.SendToConversationAsync(reply_facebook);
                            //SetActivity(reply_facebook);

                        }
                        else
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(dialogs, activity);
                            initReply.Attachments.Add(tempAttachment);
                        }
                    }
                    await connector.Conversations.SendToConversationAsync(initReply);
                }

                DateTime endTime = DateTime.Now;
                Debug.WriteLine("프로그램 수행시간 : {0}/ms", ((endTime - startTime).Milliseconds));
                Debug.WriteLine("* activity.Type : " + activity.Type);
                Debug.WriteLine("* activity.Recipient.Id : " + activity.Recipient.Id);
                Debug.WriteLine("* activity.ServiceUrl : " + activity.ServiceUrl);

                DButil.HistoryLog("* activity.Type : " + activity.ChannelData);
                DButil.HistoryLog("* activity.Recipient.Id : " + activity.Recipient.Id);
                DButil.HistoryLog("* activity.ServiceUrl : " + activity.ServiceUrl);
                DButil.HistoryLog("* activity.attachments.content.title : " + activity.Attachments);
            }
            else if (activity.Type == ActivityTypes.Message)
            {

                DButil.HistoryLog("* activity.Type == ActivityTypes.Message ");
                // userData (epkim)
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                //activity.ChannelId = "facebook";
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                try
                {
                    Debug.WriteLine("* activity.Type == ActivityTypes.Message ");
                    channelID = activity.ChannelId;
                    string orgMent = activity.Text;
                    apiFlag = "COMMON";

                    //대화 시작 시간
                    startTime = DateTime.Now;
                    long unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                    DButil.HistoryLog("*** orgMent : " + orgMent);
                    //금칙어 체크
                    CardList bannedMsg = db.BannedChk(orgMent);
                    Debug.WriteLine("* bannedMsg : " + bannedMsg.cardText);//해당금칙어에 대한 답변

                    if (bannedMsg.cardText != null)
                    {
                        Activity reply_ment = activity.CreateReply();
                        reply_ment.Recipient = activity.From;
                        reply_ment.Type = "message";
                        reply_ment.Text = bannedMsg.cardText;

                        var reply_ment_info = await connector.Conversations.SendToConversationAsync(reply_ment);
                        response = Request.CreateResponse(HttpStatusCode.OK);
                        return response;
                    }
                    else
                    {
                        Debug.WriteLine("* NO bannedMsg !");
                        queryStr = orgMent;
                        //인텐트 엔티티 검출
                        //캐시 체크
                        cashOrgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                        cacheList = db.CacheChk(cashOrgMent.Replace(" ", ""));                     // 캐시 체크 (TBL_QUERY_ANALYSIS_RESULT 조회..)


                        //캐시에 없을 경우
                        if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                        {
                            DButil.HistoryLog("cache none : " + orgMent);
                            //루이스 체크
                            cacheList.luisId = dbutil.GetMultiLUIS(orgMent);
                        }

                        if (cacheList != null && cacheList.luisIntent != null)
                        {
                            if (cacheList.luisIntent.Contains("testdrive") || cacheList.luisIntent.Contains("branch"))
                            {
                                apiFlag = "TESTDRIVE";
                            }
                            else if (cacheList.luisIntent.Contains("quot"))
                            {
                                apiFlag = "QUOT";
                            }
                            else if (cacheList.luisIntent.Contains("recommend "))
                            {
                                apiFlag = "RECOMMEND";
                            }
                            else
                            {
                                apiFlag = "COMMON";
                            }
                            DButil.HistoryLog("cacheList.luisIntent : " + cacheList.luisIntent);
                        }

                        luisId = cacheList.luisId;
                        luisIntent = cacheList.luisIntent;
                        luisEntities = cacheList.luisEntities;

                        String fullentity = db.SearchCommonEntities;    //  FN_ENTITY_ORDERBY_ADD

                        if (fullentity.Equals(""))
                        {
                            var loginStatus = userData.GetProperty<string>("loginStatus");
                            var isNumeric = int.TryParse(orgMent, out int n);
                            DButil.HistoryLog("***** loginStatus : " + loginStatus + "| isNumeric : " + isNumeric);

                            //if (loginStatus.Equals("N"))
                            if (isNumeric)
                            {
                                DButil.HistoryLog("*** loginStatus : "+loginStatus+" | activity.ChannelId : " + activity.ChannelId + " | activity.From.Id : " + activity.From.Id);
                                DButil.HistoryLog("*** orgMent : " + orgMent);
                                hrList = db.SelectHrInfo(orgMent);
                                DButil.HistoryLog("*** SelectHrInfo - tmn_cod : " + hrList[0].tmn_cod); 
                                if (hrList != null)
                                {
                                    if (hrList.Count > 0 && hrList[0].name != null)
                                    {
                                        DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name);

                                        userData.SetProperty<string>("loginStatus", "Y");
                                        userData.SetProperty<string>("tmn_cod", hrList[0].tmn_cod);
                                        userData.SetProperty<string>("workerid", hrList[0].workerid);
                                        userData.SetProperty<string>("name", hrList[0].name);
                                        userData.SetProperty<string>("eqp_typ", hrList[0].eqp_typ);
                                        userData.SetProperty<string>("eqp_typ_name", hrList[0].eqp_typ_name);                                        
                                        userData.SetProperty<string>("equipment_no", hrList[0].equipment_no);
                                        userData.SetProperty<string>("accident_record", hrList[0].accident_record);
                                        userData.SetProperty<string>("training_record", hrList[0].training_record);
                                        userData.SetProperty<string>("age", hrList[0].age);
                                        userData.SetProperty<string>("vacation", hrList[0].vacation);

                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        fullentity = "login,ok";
                                        DButil.HistoryLog("*** fullentity : " + fullentity);
                                    } 
                                    else
                                    {
                                        userData.SetProperty<string>("loginStatus", "N");
                                        userData.SetProperty<string>("tmn_cod", "");
                                        userData.SetProperty<string>("workerid", "");
                                        userData.SetProperty<string>("name", "");
                                        userData.SetProperty<string>("eqp_typ", "");
                                        userData.SetProperty<string>("eqp_typ_name", "");
                                        userData.SetProperty<string>("equipment_no", "");
                                        userData.SetProperty<string>("accident_record", "");
                                        userData.SetProperty<string>("training_record", "");
                                        userData.SetProperty<string>("age", "");
                                        userData.SetProperty<string>("vacation", "");

                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        fullentity = "fail,login";
                                        DButil.HistoryLog("*** fullentity : " + fullentity);
                                    }
                                }
                                else
                                {
                                    //  조회후 사원 번호 존재하지 않을 경우..  
                                    userData.SetProperty<string>("loginStatus", "N");
                                    userData.SetProperty<string>("tmn_cod", "");
                                    userData.SetProperty<string>("workerid", "");
                                    userData.SetProperty<string>("name", "");
                                    userData.SetProperty<string>("eqp_typ", "");
                                    userData.SetProperty<string>("eqp_typ_name", "");
                                    userData.SetProperty<string>("equipment_no", "");
                                    userData.SetProperty<string>("accident_record", "");
                                    userData.SetProperty<string>("training_record", "");
                                    userData.SetProperty<string>("age", "");
                                    userData.SetProperty<string>("vacation", "");

                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    fullentity = "fail,login";
                                    DButil.HistoryLog("*** loginStatus : N | name : " + userData.GetProperty<string>("name") + " | workerid : " + userData.GetProperty<string>("workerid"));                                    
                                    DButil.HistoryLog("*** fullentity : " + fullentity);
                                }

                            }
                            else
                            {
                                //fullentity = "login,ok";
                                DButil.HistoryLog("*** loginStatus : " + loginStatus + " | name : " + userData.GetProperty<string>("name") + "| workerid : " + userData.GetProperty<string>("workerid"));
                                DButil.HistoryLog("*** fullentity : " + fullentity);
                            }
                        }
                        
                        DButil.HistoryLog("fullentity : " + fullentity);
                        if (!string.IsNullOrEmpty(fullentity) || !fullentity.Equals(""))
                        {
                            //  fullentity 있는 경우..
                            if (!String.IsNullOrEmpty(luisEntities))
                            {
                                //entity 길이 비교
                                if (fullentity.Length > luisEntities.Length || luisIntent == null || luisIntent.Equals(""))
                                {
                                    //DefineTypeChkSpare에서는 인텐트나 루이스아이디조건 없이 엔티티만 일치하면 다이얼로그 리턴
                                    relationList = db.DefineTypeChkSpare(fullentity);
                                }
                                else
                                {
                                    relationList = db.DefineTypeChk(MessagesController.luisId, MessagesController.luisIntent, MessagesController.luisEntities);
                                }
                            }
                            else
                            {
                                relationList = db.DefineTypeChkSpare(fullentity);
                            }
                        }
                        else
                        {
                            DButil.HistoryLog("fullentity is NULL | apiFlag : "+ apiFlag + " | cacheList.luisEntities : "+ cacheList.luisEntities);
                            if (apiFlag.Equals("COMMON"))
                            {
                                //relationList = db.DefineTypeChkSpare(cacheList.luisEntities);
                                relationList = null;
                            }
                            else
                            {
                                relationList = null;
                            }
                            //DButil.HistoryLog("fullentity is NULL | relationList : " + relationList);
                        }

                        if (relationList != null)
                        {
                            if (relationList.Count > 0 && relationList[0].dlgApiDefine != null)
                            {
                                if (relationList[0].dlgApiDefine.Equals("api testdrive"))
                                {
                                    apiFlag = "TESTDRIVE";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("api quot"))
                                {
                                    apiFlag = "QUOT";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("api recommend"))
                                {
                                    apiFlag = "RECOMMEND";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("D"))
                                {
                                    apiFlag = "COMMON";
                                }
                                DButil.HistoryLog("relationList[0].dlgApiDefine : " + relationList[0].dlgApiDefine);
                            }

                        }
                        else
                        {
                            DButil.HistoryLog("relationList is NULL");
                            if (MessagesController.cacheList.luisIntent == null || apiFlag.Equals("COMMON"))
                            {
                                apiFlag = "";
                            }
                            else if (MessagesController.cacheList.luisId.Equals("kona_luis_01") && MessagesController.cacheList.luisIntent.Contains("quot"))
                            {
                                apiFlag = "QUOT";
                            }
                            DButil.HistoryLog("apiFlag : "+ apiFlag);
                        }

                        
                        if (apiFlag.Equals("COMMON") && relationList.Count > 0)
                        {
                            DButil.HistoryLog("apiFlag : COMMON | relationList.Count : " + relationList.Count);
                            //context.Call(new CommonDialog("", MessagesController.queryStr), this.ResumeAfterOptionDialog);
                            String beforeMent = "";
                            facebookpagecount = 1;
                            //int fbLeftCardCnt = 0;

                            if (conversationhistory.commonBeforeQustion != null && conversationhistory.commonBeforeQustion != "")
                            {
                                DButil.HistoryLog(fbLeftCardCnt + "{fbLeftCardCnt} :: conversationhistory.commonBeforeQustion : " + conversationhistory.commonBeforeQustion);
                                if (conversationhistory.commonBeforeQustion.Equals(orgMent) && activity.ChannelId.Equals("facebook") && fbLeftCardCnt > 0)
                                {
                                    DButil.HistoryLog("beforeMent : " + beforeMent);
                                    conversationhistory.facebookPageCount++;
                                }
                                else
                                {
                                    conversationhistory.facebookPageCount = 0;
                                    fbLeftCardCnt = 0;
                                }
                            }


                            DButil.HistoryLog("* MessagesController.relationList.Count : " + MessagesController.relationList.Count);
                            for (int m = 0; m < MessagesController.relationList.Count; m++)
                            {
                                DialogList dlg = db.SelectDialog(MessagesController.relationList[m].dlgId);
                                Activity commonReply = activity.CreateReply();
                                Attachment tempAttachment = new Attachment();
                                DButil.HistoryLog("dlg.dlgType : " + dlg.dlgType);
                                if (dlg.dlgType.Equals(CARDDLG))
                                {
                                    foreach (CardList tempcard in dlg.dialogCard)
                                    {
                                        DButil.HistoryLog("tempcard.card_order_no : " + tempcard.card_order_no);
                                        if (conversationhistory.facebookPageCount > 0)
                                        {
                                            if (tempcard.card_order_no > (MAXFACEBOOKCARDS * facebookpagecount) && tempcard.card_order_no <= (MAXFACEBOOKCARDS * (facebookpagecount + 1)))
                                            {
                                                tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                            }
                                            else if (tempcard.card_order_no > (MAXFACEBOOKCARDS * (facebookpagecount + 1)))
                                            {
                                                fbLeftCardCnt++;
                                                tempAttachment = null;
                                            }
                                            else
                                            {
                                                fbLeftCardCnt = 0;
                                                tempAttachment = null;
                                            }
                                        }
                                        else if (activity.ChannelId.Equals("facebook"))
                                        {
                                            DButil.HistoryLog("facebook tempcard.card_order_no : " + tempcard.card_order_no);
                                            if (tempcard.card_order_no <= MAXFACEBOOKCARDS && fbLeftCardCnt == 0)
                                            {
                                                tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                            }
                                            else
                                            {
                                                fbLeftCardCnt++;
                                                tempAttachment = null;
                                            }
                                        }
                                        else
                                        {
                                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                        }

                                        if (tempAttachment != null)
                                        {
                                            commonReply.Attachments.Add(tempAttachment);
                                        }
                                    }
                                }
                                else
                                {
                                    //DButil.HistoryLog("* facebook dlg.dlgId : " + dlg.dlgId);
                                    DButil.HistoryLog("* activity.ChannelId : " + activity.ChannelId);
                                    DButil.HistoryLog("* dlg.dlgId : "+ dlg.dlgId + " | dlg.cardTitle : " + dlg.cardTitle + " | dlg.cardText : " + dlg.cardText);

                                    //  userLoginOk
                                    if (dlg.cardTitle.Equals("Login OK")) //  주문내역 dialog 일시..
                                    {

                                        DButil.HistoryLog("*** activity.Conversation.Id : " + activity.Conversation.Id + " | dlg.cardText : " + dlg.cardText + " | fullentity : " + fullentity); 

                                        string[] strComment = new string[4];
                                        string optionComment = "";

                                        //strComment[0] = userData.GetProperty<string>("comp");
                                        strComment[1] = userData.GetProperty<string>("name");
                                        strComment[2] = userData.GetProperty<string>("workerid");
                                        strComment[3] = userData.GetProperty<string>("equipment_no");
                                        DButil.HistoryLog("*** strComment[0] : " + strComment[0] + " | strComment[1] : " + strComment[1] + " | strComment[2] : " + strComment[2]);

                                        optionComment = strComment[0] + "," + strComment[1] + "님(" + strComment[2] + "), " + strComment[3];
                                        //optionComment = strComment[0] + "/" + strComment[1] + "/" + fullentity;
                                        dlg.cardText = dlg.cardText.Replace("#OPTIONS", optionComment);

                                    }

                                    //  weatherInfo
                                    if (dlg.cardTitle.Equals("Weather Info")) //  주문내역 dialog 일시..
                                    {
                                        DButil.HistoryLog("*** dlg.cardTitle : " + dlg.cardTitle + " | dlg.cardText : " + dlg.cardText + " | fullentity : " + fullentity);
                                        //  GET weatherInfo..
                                        string strTime = System.DateTime.Now.ToString("yyyyMMddhh");
                                        strTime = "2018022811";
                                        weatherList = db.SelectWeatherInfo(strTime);
                                        string[] strComment = new string[3];
                                        string weatherInfo = "";
                                        if (weatherList != null)
                                        {
                                            if (weatherList.Count > 0 && weatherList[0].time != null)
                                            {
                                                DButil.HistoryLog("*** SELECT weatherList : Exist | name : " + weatherList[0].time); 
                                                for (int i = 0; i < 3; i++)
                                                {
                                                    //  10:00/19/2/2018, sunny, Rainfall 0%, Wind 1m/s, Humidity 38%
                                                    strComment[i] = weatherList[i].time.Substring(8,2)+":00/"+ weatherList[i].time.Substring(6, 2) + "/"+ weatherList[i].time.Substring(4, 2) + "/"+ weatherList[i].time.Substring(0, 4) + ", "
                                                        +" Temp "+weatherList[i].temp+ "℃, Rainfall " + weatherList[i].rainfall+"%, Wind "+weatherList[i].wind+"m/s, Humidity "+weatherList[i].humidity+"% \n";
                                                    weatherInfo = weatherInfo + "- " + strComment[i];
                                                }
                                                DButil.HistoryLog("*** weatherInfo : " + weatherInfo);
                                                dlg.cardText = dlg.cardText.Replace("#weatherInfo", weatherInfo);
                                                //DButil.HistoryLog("*** fullentity : " + fullentity);
                                            }
                                            else
                                            {
                                                //fullentity = "userFail";
                                                DButil.HistoryLog("*** weatherInfo : NO weatherList !");
                                            }
                                        }
                                        else
                                        {   
                                            //  날씨 정보 없는 경우..
                                            dlg.cardText = "There is no weather information for that day.";
                                        }
                                        //DButil.HistoryLog("*** HH : "+DateTime.Now.ToString("HH"));
                                    }

                                    //  Accident History
                                    if (dlg.cardTitle.Equals("Accident History"))
                                    {
                                        DButil.HistoryLog("*** Accident History");
                                        string strWorkerId = "";
                                        string strAccidentRecord = "";
                                        string[] strComment = orgMent.Split('\'');
                                        strWorkerId = strComment[1];
                                        DButil.HistoryLog("*** Accident History - strWorkerId : " + strWorkerId);
                                        if (strWorkerId == "")
                                        {
                                            strAccidentRecord = userData.GetProperty<string>("accident_record");
                                            DButil.HistoryLog("*** Accident History - strAccidentRecord : " + strAccidentRecord);
                                            if (strAccidentRecord == "N/A")
                                            {
                                                strAccidentRecord = "No searched history of accidents.";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#accidentHistory", strAccidentRecord);
                                        }
                                        else
                                        {
                                            hrList = db.SelectHrInfo(strWorkerId);
                                            if (hrList != null)
                                            {
                                                DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name + "| accident_record : " + hrList[0].accident_record);
                                                if (hrList.Count > 0 && hrList[0].accident_record != "N/A")
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#accidentHistory", hrList[0].accident_record);
                                                }
                                                else
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#accidentHistory", "No searched history of accidents.");
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("#accidentHistory", "No searched history of accidents.");
                                            }
                                        }
                                    }

                                    //  User Age
                                    if (dlg.cardTitle.Equals("User Age"))
                                    {
                                        DButil.HistoryLog("*** User Age");
                                        string strWorkerId = "";
                                        string strAge = "";
                                        string[] strComment = orgMent.Split('\'');
                                        strWorkerId = strComment[1];
                                        DButil.HistoryLog("*** Accident History - strWorkerId : " + strWorkerId);
                                        if (strWorkerId == "")
                                        {
                                            strAge = userData.GetProperty<string>("age");
                                            DButil.HistoryLog("*** User Age - strAge : " + strAge);
                                            if (strAge == "N/A")
                                            {
                                                strAge = "No searched history of accidents.";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#accidentHistory", strAge);
                                        }
                                        else
                                        {
                                            hrList = db.SelectHrInfo(strWorkerId);
                                            if (hrList != null)
                                            {
                                                DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name + "| age : " + hrList[0].age);
                                                if (hrList.Count > 0 && hrList[0].age != "")
                                                {
                                                    dlg.cardText = hrList[0].name + "("+ hrList[0].workerid+") : " + dlg.cardText.Replace("#userAge", hrList[0].age) + " yesars old.";
                                                }
                                                else
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#userAge", "No searched history of accidents.");
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("#userAge", "No searched history of accidents.");
                                            }
                                        }
                                    }

                                    //  Vacation
                                    if (dlg.cardTitle.Equals("Vacation"))
                                    {
                                        DButil.HistoryLog("*** Vacation");
                                        string strWorkerId = "";
                                        string strVacation = "";
                                        string[] strComment = orgMent.Split('\'');
                                        strWorkerId = strComment[1];
                                        DButil.HistoryLog("*** Vacation - strWorkerId : " + strWorkerId);
                                        if (strWorkerId == "")
                                        {
                                            strVacation = userData.GetProperty<string>("vacation");
                                            DButil.HistoryLog("*** Vacation - strVacation : " + strVacation);
                                            if (strVacation == "N/A")
                                            {
                                                strVacation = "No vacation history found.";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#vacation", strVacation);
                                        }
                                        else
                                        {
                                            hrList = db.SelectHrInfo(strWorkerId);
                                            if (hrList != null)
                                            {
                                                DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name + "| vacation : " + hrList[0].vacation);
                                                if (hrList.Count > 0 && hrList[0].vacation != "")
                                                {
                                                    dlg.cardText = hrList[0].name + "(" + hrList[0].workerid + ") : " + dlg.cardText.Replace("#vacation", hrList[0].vacation);
                                                }
                                                else
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#vacation", "No vacation history found.");
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("#vacation", "No vacation history found.");
                                            }
                                        }

                                    }

                                    //  Training History
                                    if (dlg.cardTitle.Equals("Training History"))
                                    {
                                        DButil.HistoryLog("*** Training History");
                                        string strWorkerId = "";
                                        string strTraining = "";
                                        string[] strComment = orgMent.Split('\'');
                                        strWorkerId = strComment[1];
                                        DButil.HistoryLog("*** Training History - strWorkerId : " + strWorkerId);
                                        if (strWorkerId == "")
                                        {
                                            strTraining = userData.GetProperty<string>("training_record");
                                            DButil.HistoryLog("*** Training HIstory - strTraining : " + strTraining);
                                            if (strTraining == "N/A")
                                            {
                                                strTraining = "No training history found.";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#trainingHistory", strTraining);
                                        }
                                        else
                                        {
                                            hrList = db.SelectHrInfo(strWorkerId);
                                            if (hrList != null)
                                            {
                                                DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name + "| vacation : " + hrList[0].training_record);
                                                if (hrList.Count > 0 && hrList[0].vacation != "")
                                                {
                                                    dlg.cardText = hrList[0].name + "(" + hrList[0].workerid + ") : " + dlg.cardText.Replace("#trainingHistory", hrList[0].training_record);
                                                }
                                                else
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#trainingHistory", "No training history found.");
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("#trainingHistory", "No training history found.");
                                            }
                                        }

                                    }

                                    //  Eye Sight
                                    if (dlg.cardTitle.Equals("Eye Sight"))
                                    {
                                        DButil.HistoryLog("*** Eye Sight");
                                        string strWorkerId = "";
                                        string strEyeSight = "";
                                        string[] strComment = orgMent.Split('\'');
                                        strWorkerId = strComment[1];
                                        DButil.HistoryLog("*** Eye Sight - strWorkerId : " + strWorkerId);
                                        if (strWorkerId == "")
                                        {
                                            strEyeSight = "Left(" + userData.GetProperty<string>("eye_sight_left") + "), Right(" + userData.GetProperty<string>("eye_sight_right") + ")";
                                            DButil.HistoryLog("*** Training HIstory - strEyeSight : " + strEyeSight);
                                            if (userData.GetProperty<string>("eye_sight_left") == "N/A")
                                            {
                                                strEyeSight = "No sight information.";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#eyeSight", strEyeSight);
                                        }
                                        else
                                        {
                                            hrList = db.SelectHrInfo(strWorkerId);
                                            if (hrList != null)
                                            {
                                                DButil.HistoryLog("*** SELECT hrList : Exist | name : " + hrList[0].name + "| eye_sight_left : " + hrList[0].eye_sight_left);
                                                if (hrList.Count > 0 && hrList[0].eye_sight_left != "")
                                                {
                                                    strEyeSight = "Left(" + hrList[0].eye_sight_left + "), Right(" + hrList[0].eye_sight_right + ")";
                                                    dlg.cardText = hrList[0].name + "(" + hrList[0].workerid + ") : " + dlg.cardText.Replace("#eyeSight", strEyeSight);
                                                }
                                                else
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("#eyeSight", "No sight information.");
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("#eyeSight", "No sight information.");
                                            }
                                        }

                                    }

                                    //  Accident Analysis
                                    if (dlg.cardTitle.Equals("Accident Analysis"))
                                    {
                                        DButil.HistoryLog("*** Accident Analysis - tmn_cod:" + userData.GetProperty<string>("tmn_cod")+ " | eqp_typ_name:" + userData.GetProperty<string>("eqp_typ_name"));
                                        //  PNIT, YT, Crash are related to humidity, proficiency, age. Threr is a risk of accident. (humidty 50 % ~, proficiency 5year, age 55~)
                                        string[] strAnalysis = new string[4];
                                        strAnalysis[0] = userData.GetProperty<string>("tmn_cod") + ", " + userData.GetProperty<string>("eqp_typ_name") + ", ";
                                        analysisList = db.SelectAnalysisInfo(userData.GetProperty<string>("tmn_cod"), userData.GetProperty<string>("eqp_typ_name"));

                                        if (analysisList != null)
                                        {
                                            if (analysisList.Count > 0 && analysisList[0].tmn_cod != null)
                                            {
                                                strAnalysis[0] = strAnalysis[0] + analysisList[0].accidenttype;
                                                strAnalysis[1] = analysisList[0].factor1 + ", " + analysisList[0].factor2 + "," + analysisList[0].factor3 + ". " + analysisList[0].analysis;
                                            }
                                        }
                                        dlg.cardText = dlg.cardText.Replace("#analysis1", strAnalysis[0]);
                                        dlg.cardText = dlg.cardText.Replace("#analysis2", strAnalysis[1]);

                                    }

                                    //  Accident Trend
                                    if (dlg.cardTitle.Equals("Accident Trend"))
                                    {
                                        DButil.HistoryLog("*** Accident Trend - tmn_cod:" + userData.GetProperty<string>("tmn_cod") + " | eqp_typ:" + userData.GetProperty<string>("eqp_typ"));
                                        //
                                        string[] strTrend = new string[3];
                                        string[] strCnt = { "1st", "2nd", "3rd" };
                                        string trendText = "";
                                        trendList = db.SelectTrendInfo(userData.GetProperty<string>("eqp_typ"));
                                        
                                        if (trendList != null)
                                        {
                                            if (trendList.Count > 0 && trendList[0].eqp_typ != null)
                                            {
                                                for (int i = 0; i < 3; i++)
                                                {
                                                    strTrend[i] = strCnt[i] + ". Accident type:" + trendList[i].accidenttype + ", Accident count:" + trendList[i].count;
                                                    trendText = trendText + strTrend[i];    
                                                }
                                            }
                                        }
                                        dlg.cardText = dlg.cardText.Replace("#accidentTrendList", trendText);

                                    }

                                    if (activity.ChannelId.Equals("facebook") && string.IsNullOrEmpty(dlg.cardTitle) && dlg.dlgType.Equals(TEXTDLG))
                                    {
                                        commonReply.Recipient = activity.From;
                                        commonReply.Type = "message";
                                        DButil.HistoryLog("facebook card Text : " + dlg.cardText);
                                        commonReply.Text = dlg.cardText;
                                    }
                                    else
                                    {
                                        tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                        commonReply.Attachments.Add(tempAttachment);
                                    }

                                }

                                if (commonReply.Attachments.Count > 0)
                                {
                                    DButil.HistoryLog("* commonReply.Attachments.Count : " + commonReply.Attachments.Count);

                                    SetActivity(commonReply);
                                    conversationhistory.commonBeforeQustion = orgMent;
                                    replyresult = "H";

                                }
                            }
                        }

                        /*
                        else
                        {
                            string newUserID = activity.Conversation.Id;
                            string beforeUserID = "";
                            string beforeMessgaeText = "";
                            //string messgaeText = "";

                            Activity intentNoneReply = activity.CreateReply();
                            Boolean sorryflag = false;


                            if (beforeUserID != newUserID)
                            {
                                beforeUserID = newUserID;
                                MessagesController.sorryMessageCnt = 0;
                            }

                            var message = MessagesController.queryStr;
                            beforeMessgaeText = message.ToString();

                            Debug.WriteLine("SERARCH MESSAGE : " + message);
                            //네이버 기사 검색
                            if ((message != null) && message.Trim().Length > 0)
                            {
                                //Naver Search API

                                string url = "https://openapi.naver.com/v1/search/news.json?query=" + message + "&display=10&start=1&sort=sim"; //news JSON result 
                                //string blogUrl = "https://openapi.naver.com/v1/search/blog.json?query=" + messgaeText + "&display=10&start=1&sort=sim"; //search JSON result 
                                //string cafeUrl = "https://openapi.naver.com/v1/search/cafearticle.json?query=" + messgaeText + "&display=10&start=1&sort=sim"; //cafe JSON result 
                                //string url = "https://openapi.naver.com/v1/search/blog.xml?query=" + query; //blog XML result
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                                request.Headers.Add("X-Naver-Client-Id", "Y536Z1ZMNv93Oej6TrkF");
                                request.Headers.Add("X-Naver-Client-Secret", "cPHOFK6JYY");
                                HttpWebResponse httpwebresponse = (HttpWebResponse)request.GetResponse();
                                string status = httpwebresponse.StatusCode.ToString();
                                if (status == "OK")
                                {
                                    Stream stream = httpwebresponse.GetResponseStream();
                                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                                    string text = reader.ReadToEnd();

                                    RootObject serarchList = JsonConvert.DeserializeObject<RootObject>(text);

                                    Debug.WriteLine("serarchList : " + serarchList);
                                    //description

                                    if (serarchList.display == 1)
                                    {
                                        //Debug.WriteLine("SERARCH : " + Regex.Replace(serarchList.items[0].title, @"[^<:-:>-<b>-</b>]", "", RegexOptions.Singleline));

                                        if (serarchList.items[0].title.Contains("코나"))
                                        {
                                            //Only One item
                                            List<CardImage> cardImages = new List<CardImage>();
                                            CardImage img = new CardImage();
                                            img.Url = "";
                                            cardImages.Add(img);

                                            string searchTitle = "";
                                            string searchText = "";

                                            searchTitle = serarchList.items[0].title;
                                            searchText = serarchList.items[0].description;



                                            if (activity.ChannelId == "facebook")
                                            {
                                                searchTitle = Regex.Replace(searchTitle, @"[<][a-z|A-Z|/](.|)*?[>]", "", RegexOptions.Singleline).Replace("\n", "").Replace("<:", "").Replace(":>", "");
                                                searchText = Regex.Replace(searchText, @"[<][a-z|A-Z|/](.|)*?[>]", "", RegexOptions.Singleline).Replace("\n", "").Replace("<:", "").Replace(":>", "");
                                            }


                                            LinkHeroCard card = new LinkHeroCard()
                                            {
                                                Title = searchTitle,
                                                Subtitle = null,
                                                Text = searchText,
                                                Images = cardImages,
                                                Buttons = null,
                                                Link = Regex.Replace(serarchList.items[0].link, "amp;", "")
                                            };
                                            var attachment = card.ToAttachment();

                                            intentNoneReply.Attachments = new List<Attachment>();
                                            intentNoneReply.Attachments.Add(attachment);
                                        }
                                    }
                                    else
                                    {
                                        //intentNoneReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        intentNoneReply.Attachments = new List<Attachment>();
                                        for (int i = 0; i < serarchList.display; i++)
                                        {
                                            string searchTitle = "";
                                            string searchText = "";

                                            searchTitle = serarchList.items[i].title;
                                            searchText = serarchList.items[i].description;

                                            if (activity.ChannelId == "facebook")
                                            {
                                                searchTitle = Regex.Replace(searchTitle, @"[<][a-z|A-Z|/](.|)*?[>]", "", RegexOptions.Singleline).Replace("\n", "").Replace("<:", "").Replace(":>", "");
                                                searchText = Regex.Replace(searchText, @"[<][a-z|A-Z|/](.|)*?[>]", "", RegexOptions.Singleline).Replace("\n", "").Replace("<:", "").Replace(":>", "");
                                            }

                                            if (serarchList.items[i].title.Contains("코나"))
                                            {
                                                List<CardImage> cardImages = new List<CardImage>();
                                                CardImage img = new CardImage();
                                                img.Url = "";
                                                cardImages.Add(img);

                                                List<CardAction> cardButtons = new List<CardAction>();
                                                CardAction[] plButton = new CardAction[1];
                                                plButton[0] = new CardAction()
                                                {
                                                    Value = Regex.Replace(serarchList.items[i].link, "amp;", ""),
                                                    Type = "openUrl",
                                                    Title = "기사 바로가기"
                                                };
                                                cardButtons = new List<CardAction>(plButton);

                                                if (activity.ChannelId == "facebook")
                                                {
                                                    LinkHeroCard card = new LinkHeroCard()
                                                    {
                                                        Title = searchTitle,
                                                        Subtitle = null,
                                                        Text = searchText,
                                                        Images = cardImages,
                                                        Buttons = cardButtons,
                                                        Link = null
                                                    };
                                                    var attachment = card.ToAttachment();
                                                    intentNoneReply.Attachments.Add(attachment);
                                                }
                                                else
                                                {
                                                    LinkHeroCard card = new LinkHeroCard()
                                                    {
                                                        Title = searchTitle,
                                                        Subtitle = null,
                                                        Text = searchText,
                                                        Images = cardImages,
                                                        Buttons = null,
                                                        Link = Regex.Replace(serarchList.items[i].link, "amp;", "")
                                                    };
                                                    var attachment = card.ToAttachment();
                                                    intentNoneReply.Attachments.Add(attachment);
                                                }
                                            }
                                        }
                                    }
                                    //await connector.Conversations.SendToConversationAsync(intentNoneReply);
                                    //replyresult = "S";

                                    if (intentNoneReply.Attachments.Count == 0)
                                    {
                                        sorryflag = true;
                                    }
                                    else
                                    {
                                        //await connector.Conversations.SendToConversationAsync(intentNoneReply);
                                        SetActivity(intentNoneReply);
                                        replyresult = "S";
                                    }

                                }
                                else
                                {
                                    //System.Diagnostics.Debug.WriteLine("Error 발생=" + status);
                                    sorryflag = true;
                                }
                            }
                            else
                            {
                                sorryflag = true;
                            }
                            if (sorryflag)
                            {
                                //Sorry Message 
                                int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);

                                ++MessagesController.sorryMessageCnt;

                                Activity sorryReply = activity.CreateReply();

                                sorryReply.Recipient = activity.From;
                                sorryReply.Type = "message";
                                sorryReply.Attachments = new List<Attachment>();
                                sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                                List<TextList> text = new List<TextList>();
                                if (sorryMessageCheck == 0)
                                {
                                    text = db.SelectSorryDialogText("5");
                                }
                                else
                                {
                                    text = db.SelectSorryDialogText("6");
                                }

                                for (int i = 0; i < text.Count; i++)
                                {
                                    HeroCard plCard = new HeroCard()
                                    {
                                        Title = text[i].cardTitle,
                                        Text = text[i].cardText
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    sorryReply.Attachments.Add(plAttachment);
                                }

                                SetActivity(sorryReply);
                                //await connector.Conversations.SendToConversationAsync(sorryReply);
                                sorryflag = false;
                                replyresult = "D";
                            }
                        }
                        */

                        DateTime endTime = DateTime.Now;
                        //analysis table insert
                        //if (rc != null)
                        //{
                        int dbResult = db.insertUserQuery();

                        //}
                        //history table insert

                        Debug.WriteLine("* insertHistory | Conversation.Id : " + activity.Conversation.Id + "ChannelId : " + activity.ChannelId);

                        db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds));
                        replyresult = "";
                        recommendResult = "";
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);

                    ++MessagesController.sorryMessageCnt;

                    Activity sorryReply = activity.CreateReply();

                    sorryReply.Recipient = activity.From;
                    sorryReply.Type = "message";
                    sorryReply.Attachments = new List<Attachment>();
                    //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    List<TextList> text = new List<TextList>();
                    if (sorryMessageCheck == 0)
                    {
                        text = db.SelectSorryDialogText("5");
                    }
                    else
                    {
                        text = db.SelectSorryDialogText("6");
                    }

                    for (int i = 0; i < text.Count; i++)
                    {
                        HeroCard plCard = new HeroCard()
                        {
                            Title = text[i].cardTitle,
                            Text = text[i].cardText
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        sorryReply.Attachments.Add(plAttachment);
                    }

                    SetActivity(sorryReply);

                    DateTime endTime = DateTime.Now;
                    int dbResult = db.insertUserQuery();
                    db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds));
                    replyresult = "";
                    recommendResult = "";
                }
                finally
                {
                    // facebook 환경에서 text만 있는 멘트를 제외하고 carousel 등록
                    if (!(activity.ChannelId == "facebook" && reply1.Text != ""))
                    {
                        reply1.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    }
                    if (!(activity.ChannelId == "facebook" && reply2.Text != ""))
                    {
                        reply2.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    }
                    if (!(activity.ChannelId == "facebook" && reply3.Text != ""))
                    {
                        reply3.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    }
                    if (!(activity.ChannelId == "facebook" && reply4.Text != ""))
                    {
                        reply4.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    }

                    

                    if (reply1.Attachments.Count != 0 || reply1.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply1);
                    }
                    if (reply2.Attachments.Count != 0 || reply2.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply2);
                    }
                    if (reply3.Attachments.Count != 0 || reply3.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply3);
                    }
                    if (reply4.Attachments.Count != 0 || reply4.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply4);
                    }

                    //페이스북에서 남은 카드가 있는경우
                    if (activity.ChannelId.Equals("facebook") && fbLeftCardCnt > 0)
                    {
                        Activity replyToFBConversation = activity.CreateReply();
                        replyToFBConversation.Recipient = activity.From;
                        replyToFBConversation.Type = "message";
                        replyToFBConversation.Attachments = new List<Attachment>();
                        replyToFBConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                        replyToFBConversation.Attachments.Add(
                            GetHeroCard_facebookMore(
                            "", "",
                            fbLeftCardCnt + "개의 컨테츠가 더 있습니다.",
                            new CardAction(ActionTypes.ImBack, "더 보기", value: MessagesController.queryStr))
                        );
                        await connector.Conversations.SendToConversationAsync(replyToFBConversation);
                        replyToFBConversation.Attachments.Clear();
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            response = Request.CreateResponse(HttpStatusCode.OK);
            return response;

        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.Typing)
            {
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            return null;
        }

        private static Attachment GetHeroCard_facebookMore(string title, string subtitle, string text, CardAction cardAction)
        {
            var heroCard = new UserHeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Buttons = new List<CardAction>() { cardAction },
            };
            return heroCard.ToAttachment();
        }
    }
}