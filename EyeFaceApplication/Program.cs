////////////////////////////////////////////////////////////////////////////////////////
//                                                                                    //
//    C# EyeFace SDK Example                                                          //
// ---------------------------------------------------------------------------------- //
//                                                                                    //
// Copyright (c) 2017 by Eyedea Recognition, s.r.o.                                   //
//                                                                                    //
// Author: Eyedea Recognition, s.r.o.                                                 //
// Second Author: Rudja RULLE    
//
// Contact:                                                                           //
//           web: http://www.eyedea.cz                                                //
//           email: info@eyedea.cz 
//           email: rudja971@hotmail.com
//                                                                                    //
// BSD License                                                                        //
// -----------------------------------------------------------------------------------//
// Copyright (c) 2017, Eyedea Recognition, s.r.o.                                     //
// All rights reserved.                                                               //
// Redistribution and use in source and binary forms, with or without modification,   // 
// are permitted provided that the following conditions are met :                     //
//     1. Redistributions of source code must retain the above copyright notice,      //
//        this list of conditions and the following disclaimer.                       //
//     2. Redistributions in binary form must reproduce the above copyright notice,   //
//        this list of conditions and the following disclaimer in the documentation   //
//        and / or other materials provided with the distribution.                    //
//     3. Neither the name of the copyright holder nor the names of its contributors  //
//        may be used to endorse or promote products derived from this software       //
//        without specific prior written permission.                                  //
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"        //
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED  //
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. //
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,   //
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES(INCLUDING, BUT  //
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR //
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,  //
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT(INCLUDING NEGLIGENCE OR OTHERWISE)  //
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF             //
// THE POSSIBILITY OF SUCH DAMAGE.                                                    //
////////////////////////////////////////////////////////////////////////////////////////


using System;
using Eyedea.EyeFace;
using Eyedea.er;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json.Linq;
using System.Threading;

using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace EyeFaceApplication
{
    class Program
    {
        private const string EYEFACE_DIR = "..\\..\\eyefacesdk";
        private const string CONFIG_INI = "config.ini";

        //My constants
        private const double limitTime = -300000;                            //After an inactivity of limitTime minute, the system consider that this is a new interaction  
        private const string ProjectName = "3D Modeler";                //Name of the project where the application is installed
        private const int adjust_variable_for_attentionTime = 75;       //To compensate the difference between real attention_time and the one given by the sdk

        public static int Main(string[] args)
        {
            try
            {
                //Automatic facial recognition
                efEyeFaceStandardExample();        
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR: efEyeFaceStandardExample() failed: " + e.ToString());
                //If you want to capture errors in terminal
                //Console.Read();
                return -1;
            }
            return 0;
        }

        //Functions that can be used for save taken image into a directory
        private static void renderAndSaveImage(ERImage image,
                                               EfTrackInfoArray trackinfoArray,
                                               EfCsSDK efCsSDK, string imageSavePath)
        {
            Bitmap bitmap = efCsSDK.erImageToCsBitmap(image);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(Color.White, 2.0f);
                for (int i = 0; i < trackinfoArray.num_tracks; i++)
                {
                    EfTrackInfo trackinfo = trackinfoArray.track_info[i];
                    EfBoundingBox detBBox = trackinfo.image_position;
                    g.DrawPolygon(pen, detBBox.Points);
                    string text = "Track: " + trackinfo.track_id;
                    float fontSize = 8.0f;
                    Font font = new Font(new FontFamily("Arial"), fontSize);
                    SolidBrush brush = new SolidBrush(Color.White);
                    g.DrawString(text, font, brush, detBBox.top_left_col, detBBox.top_left_row - 2.0f * fontSize);
                }
            }

            bitmap.Save(imageSavePath);
        }

        private static void renderAndSaveImage(ERImage image,
                                               EfDetectionArray detectionArray, EfTrackInfoArray trackinfoArray,
                                               EfCsSDK efCsSDK, string imageSavePath)
        {
            Bitmap bitmap = efCsSDK.erImageToCsBitmap(image);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(Color.White, 2.0f);
                for (int i = 0; i < detectionArray.num_detections; i++)
                {
                    EfDetection detection = detectionArray.detections[i];
                    g.DrawPolygon(pen, detection.position.bounding_box.Points);
                }

                for (int i = 0; i < trackinfoArray.num_tracks; i++)
                {
                    EfTrackInfo trackinfo = trackinfoArray.track_info[i];
                    if (trackinfo.detection_index == -1)
                    {
                        continue;
                    }
                    EfDetection detection = detectionArray.detections[trackinfo.detection_index];
                    EfBoundingBox detBBox = detection.position.bounding_box;
                    string text = "Track: " + trackinfo.track_id;
                    float fontSize = 8.0f;
                    Font font = new Font(new FontFamily("Arial"), fontSize);
                    SolidBrush brush = new SolidBrush(Color.White);
                    g.DrawString(text, font, brush, detBBox.top_left_col, detBBox.top_left_row - 2.0f * fontSize);
                }
            }

            bitmap.Save(imageSavePath);
        }

        private static int checkSatisfaction(EfEmotionClass emotion)
        {
            if (emotion == EfEmotionClass.EF_EMOTION_SMILING)
            {
                //Return 1 if the person is smiling
                return 1;
            } else
            {
                //Return 0 if the person is not smiling
                return 0;
            }
        }

        private static void saveDataIntoEyeFaceDB(People new_person)
        {
            //Connection to our database
            var connectionString = "mongodb://localhost";
            MongoClient client = new MongoClient(connectionString);
            var db = client.GetDatabase("EyeFaceDB");
            var collection = db.GetCollection<People>("People");

            var filter = Builders<People>.Filter.Eq("person_id", new_person.person_id);
            var result = collection.Find(filter).ToList();

            if (result.Count() != 0)
            {
                //This person already exist in the database
                //Now we will check if their was already an attraction with THIS project in the limitTime
                var builder = Builders<People>.Filter;
                filter = builder.Eq("person_id", new_person.person_id)
                         & builder.Eq("attractions.project_name", new_person.attractions[0].project_name)
                         & builder.Gt("attractions.dateUTC", DateTime.UtcNow.AddMilliseconds(limitTime));
                //& builder.Eq("attractions.satisfied", 0);
                result = collection.Find(filter).ToList();
                //Console.Write("Number of attraction on same project in this our : ");
                //Console.WriteLine(result.Count());
                if (result.Count() != 0)
                {
                    //We found normally one table where the user stay on the same project.
                    //We will update it with the new value of satisfied variable if he is satisfied now.
                    Console.WriteLine("Let's see if you are satisfied now!");
                    //Console.WriteLine(satisfaction);
                    if (new_person.attractions[0].satisfied == 1)
                    {
                        //I decided to only write 1 in the database. 
                        //If the person was not smiling during the interaction, the final user will only see 0 in database. 
                        Console.WriteLine("You are now smiling!");
                        var update = Builders<People>.Update.Set("attractions.$.satisfied", new_person.attractions[0].satisfied)
                                                            .Set("attractions.$.attention_time", new_person.attractions[0].attention_time);
                        var result1 = collection.UpdateOne(filter, update);
                    }
                    else
                    {
                        //We do nothing and let this field to 0 or 1 during the limitTime. 
                        //After this hour, we will not be able to change it anymore, so we will have the real
                        //result about the fact that this person smile or not during his experience
                        Console.WriteLine("You are NOT smiling!");
                        var update = Builders<People>.Update.Set("attractions.$.attention_time", new_person.attractions[0].attention_time);
                        var result1 = collection.UpdateOne(filter, update);
                    }
                }
                else
                {
                    //We will create a new attraction row because this person is coming back
                    //to this project. In my point of view this is a new interaction.
                    Console.WriteLine("You were not on this project recently");
                    Attraction new_Attraction = new Attraction()
                    {
                        project_name = new_person.attractions[0].project_name,
                        dateUTC = DateTime.UtcNow,
                        attention_time = new_person.attractions[0].attention_time,
                        satisfied = new_person.attractions[0].satisfied
                    };
                    filter = builder.Eq("person_id", new_person.person_id);
                    var update = Builders<People>.Update.Push("attractions", new_Attraction);
                    collection.FindOneAndUpdate(filter, update);
                }
            }
            else
            {
                collection.InsertOne(new_person);
            }
        }


        /// <summary>
        /// This is a C# version of EyeFace Standard API example on how to process a videostream.
        /// </summary>

        public static void efEyeFaceStandardExample()
        {
            // then instantiate EfCsSDK object
            EfCsSDK efCsSDK = new EfCsSDK(EYEFACE_DIR);
            System.Console.WriteLine("EyeFace C# interface initialized.");

            // init EyeFace                                               
            System.Console.Write("EyeFace init ... ");
            bool initState = efCsSDK.efInitEyeFace(EYEFACE_DIR, EYEFACE_DIR, CONFIG_INI);
            if (!initState)
            {
                System.Console.Error.WriteLine("Error during EyeFace initialization.");
                return;
            }
            System.Console.WriteLine("done.\n");

            //create a camera capture. Work with last EmguCV version 3.2
            //We can select the camera index as parameter in VideoCapture function
            VideoCapture capture = new VideoCapture();                                  
            Mat captureFrame = null;

            int iImgNo = 0;
            bool firstIteration = true;

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                //For incrementation of iImgNo variable
                if (firstIteration)
                {
                    //Do nothing
                    firstIteration = false;
                }
                else
                {
                    iImgNo++;
                }

                //Get a frame from capture and convert it into a bitmap image
                //I try to get this bitmap because eyeface sdk have a convertor image (bitmap->Erimage)
                captureFrame = capture.QueryFrame();                                            //I got some access violation here.   
                Image<Bgr, Byte> myImage = captureFrame.ToImage<Bgr, Byte>();
                Bitmap myBitmap = myImage.ToBitmap();

                // load image
                ERImage image;
                try
                {
                    image = efCsSDK.csBitmapToERImage(myBitmap);
                }
                catch (ERException)
                {
                    System.Console.Error.WriteLine("Can't load the file: ");
                    return;
                }
                System.Console.WriteLine("done.");

                // setup detection area                                         
                System.Console.Write("    Face detection ... ");
                EfBoundingBox bbox = new EfBoundingBox(image.width, image.height);

                // run face detector 
                double frameTime = Convert.ToDouble(iImgNo) / 10.0;
                bool detectionStatus = efCsSDK.efMain(image, bbox, frameTime);
                if (!detectionStatus)
                {
                    System.Console.Error.WriteLine("Error during detection on image " + iImgNo.ToString() + ".");
                    efCsSDK.erImageFree(ref image);
                    return;
                }
                System.Console.WriteLine("done.\n");

                // Get track infos object from the image
                EfTrackInfoArray trackInfoArray = efCsSDK.efGetTrackInfo();

                /// We will create a People object and fill it with the data given by eyeface SDK
                /// Before we need to verify that all datas are set before stocking them
                /// We free the image taken when we are done with it. 
                /// Finally we send the People object to our database
                JTokenWriter person = new JTokenWriter();
                for (int i = 0; i < trackInfoArray.num_tracks; i++)         //num_tracks equal to the number of person detected on the image
                {
                    EfTrackInfo track_info = trackInfoArray.track_info[i];

                    //Verify if all parameters are set before formatting them into a JSON object
                    if (trackInfoArray.track_info[i].person_id != 0 && trackInfoArray.track_info[i].face_attributes.gender.recognized
                                                                    && trackInfoArray.track_info[i].face_attributes.age.recognized
                                                                    && trackInfoArray.track_info[i].face_attributes.emotion.recognized
                                                                    && trackInfoArray.track_info[i].face_attributes.ancestry.recognized)
                    {
                        //*****************************************************
                        //Here you can visualize your data 
                        //Save data into the JSON
                        Console.WriteLine("Data in the JSON \n");
                        person.WriteStartObject();

                        person.WritePropertyName("PersonID");
                        person.WriteValue(trackInfoArray.track_info[i].person_id);

                        person.WritePropertyName("Gender");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.gender.ToString());

                        person.WritePropertyName("Age");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.age.value);

                        person.WritePropertyName("Emotion");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.emotion.ToString());

                        person.WritePropertyName("Ancestry");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.ancestry.ToString());

                        person.WritePropertyName("AttentionTime");
                        person.WriteValue(trackInfoArray.track_info[i].attention_time);

                        person.WritePropertyName("ProjectName");
                        person.WriteValue(ProjectName);

                        person.WriteEndObject();

                        JObject o = (JObject)person.Token;
                        Console.WriteLine(o.ToString());
                        //******************************************************

                        //Here we fill a person object to send it to the save funtion into database
                        Attraction person_Attraction = new Attraction()
                        {
                            project_name = ProjectName,
                            dateUTC = DateTime.UtcNow,
                            attention_time = (int)trackInfoArray.track_info[i].attention_time,
                            satisfied = checkSatisfaction(trackInfoArray.track_info[i].face_attributes.emotion.value)
                        };
                        List<Attraction> myList = new List<Attraction>();
                        myList.Add(person_Attraction);

                        People new_person = new People
                        {
                            person_id = (int)trackInfoArray.track_info[i].person_id,
                            gender = trackInfoArray.track_info[i].face_attributes.gender.ToString(),
                            age = (int)trackInfoArray.track_info[i].face_attributes.age.value,
                            ancestry = trackInfoArray.track_info[i].face_attributes.ancestry.ToString(),
                            attractions = myList
                        };

                        // free the image
                        efCsSDK.erImageFree(ref image);

                        saveDataIntoEyeFaceDB(new_person);
                        //Thread.Sleep(adjust_variable_for_attentionTime);
                    }
                    else
                    {
                        Console.WriteLine("Data not set yet...");
                    }
                }
            }

            // shutdown EyeFace SDK to force all tracks to finish and gather final results.
            efCsSDK.efShutdownEyeFace();

            System.Console.WriteLine("[Press ENTER to exit]");
            System.Console.ReadLine();
        }
    }
}
