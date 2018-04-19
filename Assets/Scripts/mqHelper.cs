using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using PlayerJson;
using CymaticLabs.Unity3D.Amqp;


namespace MqHelper {

    public class MqHelper {
        public static void test() {
            AmqpClient.Connect();
            AmqpClient.DeclareQueue("hello");
            AmqpClient.Publish("hello", "hello", "from unity message");
            Debug.Log("MqHelper test()");
        }
    }
}
