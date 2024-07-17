using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Net;
using System.IO;
using System.Xml;
using System.Reflection.Emit;

namespace wallpade
{
    public partial class Form1 : Form
    {
        //전역으로 클래스 선언
        MqttClient client;
        string clientid;
        Color color = ColorTranslator.FromHtml("#208FF4"); //on
        Color color2 = ColorTranslator.FromHtml("#2F2F3D"); //off
        private bool pantype = false;
        private bool led0 = false;
        private bool led1 = false;
        private bool led2 = false;
        private bool led3 = false;
        private bool music = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //날짜 및 시간
            timer1.Interval = 100;
            timer1.Start();
            label7.Text = DateTime.Now.ToString("yyyy년 MM월 dd일 dddd");
            //날씨 및 기온
            try
            {
                // 기상청 RSS API URL
                string query = "https://www.weather.go.kr/w/rss/dfs/hr1-forecast.do?zone=2644055000";

                WebRequest wr = WebRequest.Create(query);
                wr.Method = "GET";

                // Response를 받는다!
                WebResponse wrs = wr.GetResponse();
                Stream s = wrs.GetResponseStream();
                StreamReader sr = new StreamReader(s);

                string response = sr.ReadToEnd();

                // response 받은 것을 xml 파싱한다!
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(response);

                // 데이터 처리하는 부분
                XmlNodeList dataList = xd.SelectNodes("/rss/channel/item/description/body/data");
                XmlNodeList datatitle = xd.SelectNodes("/rss/channel/item");
                if (dataList != null && dataList.Count > 0)
                {
                    // 시간대 별로 정보 표시
                    foreach (XmlNode node in datatitle)
                    {
                        string title = node.SelectSingleNode("category").InnerText;
                        label16.Text = title;

                    }
                    // 시간대 별로 정보 표시
                    foreach (XmlNode data in dataList)
                    {
                        double tempDouble = double.Parse(data.SelectSingleNode("temp").InnerText);  // 기온을 double로 파싱
                        int tmn = (int)tempDouble;
                        string wf = data.SelectSingleNode("wfKor").InnerText;  // 날씨


                        // 각 정보를 label에 표시
                        if (wf == "맑음")
                        {
                            pictureBox7.Image = Properties.Resources.ph_sun1;
                        }
                        else if (wf == "흐림")
                        {
                            pictureBox7.Image = Properties.Resources.fluent_weather_partly_cloudy_day_48_regular;
                        }
                        else if (wf == "구름 많음")
                        {
                            pictureBox7.Image = Properties.Resources.material_symbols_cloud_outline1;
                        }
                        label15.Text = tmn + "°";
                        label9.Text = wf;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            //mqtt 브로커와 연결
            string BrokerAddress = "10.150.151.254";
            client = new MqttClient(BrokerAddress);

            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

            clientid = Guid.NewGuid().ToString();
            client.Connect(clientid);


            string[] mytopic =
            {
                "/topic/dht",
                "/topic/led0",
                "/topic/led1",
                "/topic/led2",
                "/topic/led3",
                "/topic/motor",
                "/topic/buzz",
                "/topic/read_sw",
            };

            byte[] myqos =
            {
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
            };

            client.Subscribe(mytopic, myqos);



        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string RecivedMsg = Encoding.UTF8.GetString(e.Message);


            if(e.Topic == "/topic/dht")
            {
                try
                {
                    //json 파싱
                    JObject myjson = JObject.Parse(RecivedMsg);
                    label11.Text = myjson["temp"].ToString() +".00℃";
                    label12.Text = myjson["humi"].ToString() +".00％";

                }
                catch
                {

                }
            }
            if (e.Topic == "/topic/read_sw")
            {
                try
                {
                    //json 파싱
                    JObject myjson = JObject.Parse(RecivedMsg);
                    if (myjson["read_sw"].ToString() == "1")
                    {
                        panel2.BackColor = color2;
                        label13.Text = "CLOSE";
                    }
                    else if (myjson["read_sw"].ToString() == "0")
                    {
                        panel2.BackColor = color;
                        label13.Text = "OPEN";
                    }

                }
                catch
                {

                }
            }

            //throw new NotImplementedException();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Disconnect();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label8.Text = DateTime.Now.ToString("hh:mm");
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //환풍기
            pantype = !pantype;
            if (pantype == true)
            {
                panel3.BackColor = color2;
                label14.Text = "OFF";
                client.Publish("/topic/motor", Encoding.UTF8.GetBytes("0"), 0, false);//팬 끄기
            }
            else if (pantype == false)
            {
                panel3.BackColor = color;
                label14.Text = "ON";
                client.Publish("/topic/motor", Encoding.UTF8.GetBytes("1"), 0, false);//팬 켜기
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //전등1
            led0 = !led0;
            if (led0 == true)
            {
                panel4.BackColor = color2;
                pictureBox3.Image = Properties.Resources.iconoir_light_bulb;
                client.Publish("/topic/led0", Encoding.UTF8.GetBytes("0"), 0, false);//LED 끄기
            }
            else if (led0 == false)
            {
                panel4.BackColor = color;
                pictureBox3.Image = Properties.Resources.iconoir_light_bulb_on;
                client.Publish("/topic/led0", Encoding.UTF8.GetBytes("1"), 0, false);//LED 켜기
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //전등2
            led1 = !led1;
            if (led1 == true)
            {
                panel8.BackColor = color2;
                pictureBox4.Image = Properties.Resources.iconoir_light_bulb;
                client.Publish("/topic/led1", Encoding.UTF8.GetBytes("0"), 0, false);//LED 끄기
            }
            else if (led1 == false)
            {
                panel8.BackColor = color;
                pictureBox4.Image = Properties.Resources.iconoir_light_bulb_on;
                client.Publish("/topic/led1", Encoding.UTF8.GetBytes("1"), 0, false);//LED 켜기
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            //전등3
            led2 = !led2;
            if (led2 == true)
            {
                panel5.BackColor = color2;
                pictureBox5.Image = Properties.Resources.iconoir_light_bulb;
                client.Publish("/topic/led2", Encoding.UTF8.GetBytes("0"), 0, false);//LED 끄기
            }
            else if (led2 == false)
            {
                panel5.BackColor = color;
                pictureBox5.Image = Properties.Resources.iconoir_light_bulb_on;
                client.Publish("/topic/led2", Encoding.UTF8.GetBytes("1"), 0, false);//LED 켜기
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            //거실
            led3 = !led3;
            if (led3 == true)
            {
                panel6.BackColor = color2;
                pictureBox6.Image = Properties.Resources.iconoir_light_bulb;
                client.Publish("/topic/led3", Encoding.UTF8.GetBytes("0"), 0, false);//LED 끄기
            }
            else if (led3 == false)
            {
                panel6.BackColor = color;
                pictureBox6.Image = Properties.Resources.iconoir_light_bulb_on;
                client.Publish("/topic/led3", Encoding.UTF8.GetBytes("1"), 0, false);//LED 켜기
            }

        }
    }
}
