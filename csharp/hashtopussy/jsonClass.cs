﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Collections;
using System.Web.Script.Serialization;
using System.Linq;
using System.Threading;

public class jsonClass
{

    public Boolean debugFlag { get; set; }
    public string connectURL { get; set; }
    Random rnd = new Random(Guid.NewGuid().GetHashCode()); //init and seed the random generator for use in re-try backdown 
    JavaScriptSerializer jss = new JavaScriptSerializer();
    //Checks if json string has success response
    //Will print the error messages on fail
    public Boolean isJsonSuccess(string jsonString)
    {
        if (debugFlag)
            Console.WriteLine(jsonString);

        try
        {
            Dictionary<string, dynamic> dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonString);

            if (dict.ContainsKey("response"))
            {
                if (dict["response"] == "SUCCESS")
                {
                    return true;
                }
                else
                {
                    Console.WriteLine(dict["response"]);
                    if (dict.ContainsKey("message"))
                    {
                        Console.WriteLine(dict["message"]);
                    }
                }
            }
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Data);
            Console.WriteLine("Empty string for success check");
            return false;
        }

    }

    //Returns variable from json string, values are casted to string
    public string getRetVar(string jsonString, string itemVar)
    {

        var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonString);
        if (dict.ContainsKey(itemVar))
        {
            return Convert.ToString(dict[itemVar]);
        }

        return "NULL";
    }

    //Returns json string array to arraylist
    //This is probably redundant as we can use the below function gerRetList to return a better typed array
    public ArrayList getRetArray(string jsonString, string itemVar)
    {

        var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonString);
        if (dict.ContainsKey(itemVar))
        {
            return dict[itemVar];
        }

        return null;
    }

    //Return json string array to list with type string
    public List<string> getRetList(string jsonString, string itemVar)
    {

        var dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonString);
        if (dict.ContainsKey(itemVar))
        {
            List<string> newList = new List<string>(dict[itemVar].ToArray(typeof(string))); //Convert Array to List<T>
            return newList;
        }

        return dict[itemVar];
    }

    //Converts array=>key to jason string format
    public string toJson(object obj)
    {

        var json = jss.Serialize(obj);
        if (debugFlag)
            Console.WriteLine(json);
        return json;
    }


    public string jsonSendOnce(string json)
    {
        var request = (HttpWebRequest)WebRequest.Create(connectURL);
        request.ContentType = "application/x-www-form-urlencoded";
        request.Method = "POST";
        request.KeepAlive = false;

        int randomTime = 0;

        HttpWebResponse response = null;
        int tries = 0;
        {
            Thread.Sleep(tries * 1000 + randomTime * 1000);
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write("query=" + json);
                }

            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                return null ;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            try
            {
                response = (HttpWebResponse)request.GetResponse();
                string result;
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                return result;
            }
            catch(WebException ex)
            {
                Console.WriteLine(ex.Message);
            }


            return null;

        }
    }

    //On fail, the client will use a backdown algorithm and retry 30 times
    public string jsonSend(string json)
    {
        var request = (HttpWebRequest)WebRequest.Create(connectURL);
        request.ContentType = "application/x-www-form-urlencoded";
        request.Method = "POST";
        request.KeepAlive = false;
 
        int randomTime = 0;

        HttpWebResponse response = null;
        int tries = 0;
        do
        {
            Thread.Sleep(tries * 1000 + randomTime * 1000);
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write("query=" + json);
                }

            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                tries++;
                randomTime = rnd.Next(1, tries);
                Console.WriteLine("Attempting to re-connect in {0} seconds", tries + randomTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
            }
            break;
        } while (tries <= 30);


        tries = 0;
        randomTime = 0;
        do
        {
            Thread.Sleep(tries * 1000 + randomTime * 1000);
            try
            {
                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("Invalid HTTP response");
                    Console.WriteLine("terminating");
                    Environment.Exit(0);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    Console.WriteLine("Server timed out");
                    tries++;
                    randomTime = rnd.Next(1, tries);
                    Console.WriteLine("Attempting to re-connect in {0} seconds", tries + randomTime);
                }
                else
                {
                    Console.WriteLine("We are here");
                    throw;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not connect to specified server, exiting");
                Environment.Exit(0);
            }
            break;
        } while (tries <= 10);



        string result;
        using (var streamReader = new StreamReader(response.GetResponseStream()))
        {
            result = streamReader.ReadToEnd();
        }
        if (string.IsNullOrEmpty(result))
        {
            Console.WriteLine("server is not responding to requests");
            Console.WriteLine("terminating");
            Environment.Exit(0);
        }

        return result; //Return json string

    }


    public jsonClass()
    {
    }
}
