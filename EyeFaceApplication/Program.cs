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
using System.Data.SqlClient;
using System.Threading;

namespace EyeFaceApplication
{
    class Program
    {
        private const string EYEFACE_DIR = "..\\..\\eyefacesdk";
        //private const string EYEFACE_DIR = "C:\\Users\\Innovation\\Documents\\GitHub\\EyeFaceApplication\\eyefacesdk";
        private const string CONFIG_INI = "config.ini";

        public static int Main(string[] args)
        {
            try
            {
                //efEyeFaceStandardExample();


                int personID = 1;
                int projectID = 3;
                int attention_time = 0;
                int milliseconds = 1000;
           
                while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    JTokenWriter person = new JTokenWriter();
                    //Save data into the JSON
                    Console.WriteLine("Data in the JSON \n");
                    person.WriteStartObject();

                    person.WritePropertyName("PersonID");
                    person.WriteValue(personID);

                    person.WritePropertyName("Gender");
                    person.WriteValue("Male");

                    person.WritePropertyName("Age");
                    person.WriteValue(23);

                    person.WritePropertyName("Emotion");
                    person.WriteValue("Smiling");

                    person.WritePropertyName("Ancestry");
                    person.WriteValue("Africain");

                    person.WritePropertyName("Attention_time");
                    person.WriteValue(attention_time);

                    person.WritePropertyName("ProjectName");
                    person.WriteValue("3D Modeler");

                    person.WriteEndObject();

                    JObject o = (JObject)person.Token;
                    Console.WriteLine(o.ToString());

                    saveDataIntoEyeFaceDB(o);

                    attention_time += 1;

                    Thread.Sleep(milliseconds);
                }



                
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR: efEyeFaceStandardExample() failed: " + e.ToString());
                return -1;
            }
            return 0;
        }

        //Functions that can be use for save taken image into a directory
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

        private static bool satisfactionFunction(SqlConnection cnn, string emotion)
        {
            string cmdString = "INSERT INTO dbo.People (PersonID, Gender, Ancestry, Age) VALUES (@personID, @gender, @ancestry, @age)";

            bool result = false;
            if (emotion == "Smiling")
            {
                return result = true;
            } else
            {
                return result = false;
            }
        }

        private static int retrievePersonIDFunction(SqlConnection cnn, int personID)
        {
            SqlCommand retrieve_Person_ID = new SqlCommand("SELECT ID_People FROM People WHERE ([PersonID] = @person_ID)", cnn);
            retrieve_Person_ID.Parameters.AddWithValue("@person_ID", personID);
            return (int)retrieve_Person_ID.ExecuteScalar();
        }

        private static int retrieveProjectIDFunction(SqlConnection cnn, string project_Name)
        {
            SqlCommand retrieve_Project_ID = new SqlCommand("SELECT ID_Projects FROM Projects WHERE ([Name] = @project_Name)", cnn);
            retrieve_Project_ID.Parameters.AddWithValue(@project_Name, project_Name);
            return (int)retrieve_Project_ID.ExecuteScalar();
        }

        private static void saveDataIntoEyeFaceDB(JObject o)
        {
            DateTime previousDate = DateTime.MinValue;
            //Variables for the Persons table  
            int personID = (int)o.GetValue("PersonID");
            string gender = o.GetValue("Gender").ToString();
            string ancestry = o.GetValue("Ancestry").ToString();
            int age = (int)o.GetValue("Age");
            string emotion = o.GetValue("Emotion").ToString();
            DateTime dateUTC = DateTime.UtcNow;

            //satisfied and satisfactionNow are int because we can not store boolean type into database.
            int satisfied = 0;
            int satisfactionNow = 0;
            float attention_time = 0;
            string projectName = (string)o.GetValue("ProjectName");

            int id_project = 0;
            int id_people = 0;

            //Check the value of emotion
            if (emotion == "Smiling")
            {
                satisfactionNow = 1;
            } else
            {
                satisfactionNow = 0;
            }

            string connectionString = null;
            connectionString = "Server= DESKTOP-G255T7U\\SQLEXPRESS; Database= EyeFaceDB;Trusted_Connection=true";
            SqlConnection cnn = new SqlConnection(connectionString);

            string cmdString = "INSERT INTO dbo.People (PersonID, Gender, Ancestry, Age) VALUES (@personID, @gender, @ancestry, @age)" +
                               "INSERT INTO dbo.Attractions (DateUTC, Satisfied, Attention_Time, PersonID, ProjectName)" +
                               "VALUES (@DateUTC, @Satisfied, @Attention_Time, @PersonID, @ProjectID);";

            //String to delete all data from a table
            //string cmdStringPerson = "DELETE FROM Persons";

            cnn.Open();
            id_people = retrievePersonIDFunction(cnn, personID);
            id_project = retrieveProjectIDFunction(cnn, projectName);

            //Check if the person already exists or not
            SqlCommand check_PersonID = new SqlCommand("SELECT * FROM People WHERE ([PersonID] = @personID)", cnn);
            check_PersonID.Parameters.AddWithValue("@personID", personID);
            SqlDataReader reader = check_PersonID.ExecuteReader();
            if (!reader.HasRows)
            {
                //User does not exist. We will create a table for him. (People and Attractions table)
                reader.Close();
                using (SqlCommand comm = new SqlCommand(cmdString, cnn))
                {
                    try
                    {
                        //cnn.Open();
                        comm.Parameters.AddWithValue("@personID", personID);
                        comm.Parameters.AddWithValue("@gender", gender);
                        comm.Parameters.AddWithValue("@ancestry", ancestry);
                        comm.Parameters.AddWithValue("@age", age);

                        comm.Parameters.AddWithValue("@DateUTC", dateUTC);
                        comm.Parameters.AddWithValue("@Satisfied", satisfactionNow);
                        comm.Parameters.AddWithValue("@Attention_Time", attention_time);
                        comm.Parameters.AddWithValue("@PersonID", id_people);
                        comm.Parameters.AddWithValue("@ProjectName", id_project);
                        comm.ExecuteNonQuery();
                        Console.WriteLine("This person have been saved!");
                        cnn.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            else
            {
                //User already exists. We will create or update an Attractions table. 
                //We need to catch the correct Attractions table if exists. 
                reader.Close();
                Console.WriteLine("This person already exists in the database.");
                try
                {
                    //Check if the last resutl was in the last hour or not
                    SqlCommand find_Attractions_table = new SqlCommand("SELECT DateUTC, Satisfied FROM Attractions WHERE ([ID_People] = @PersonID AND [ID_Projects] = @ProjectID)", cnn);
                    find_Attractions_table.Parameters.AddWithValue("@PersonID", id_people);
                    find_Attractions_table.Parameters.AddWithValue("@ProjectID", id_project);

                    SqlDataReader read_Hour_Satisfaction = find_Attractions_table.ExecuteReader();
                    while (read_Hour_Satisfaction.Read())
                    {
                        //While we find corresponding Attractions table
                        previousDate = (DateTime)read_Hour_Satisfaction[0];
                        satisfied = (int)read_Hour_Satisfaction[1];  //In database, satisfied is a int field

                        if ((dateUTC - previousDate).Hours > 1)
                        {
                            //If there is less than 1 hour between now and the attraction date
                            //We update the "Satisfied" field
                            //Check if this person have already smiled in the hour and on this project
                            if (satisfied == 1)
                            {
                                //We do nothing because the user was already satisfied
                            }
                            else
                            {
                                //The user was not satisfied yet. So we update the field [Satisfied], of the current table
                                SqlCommand update_Satisfied_Field = new SqlCommand("UPDATE Attractions SET [Satisfied] = @Satisfied WHERE ([PersonID] = @personID AND" +
                                                   "[ProjectID] = @projectID)", cnn);
                                update_Satisfied_Field.Parameters.AddWithValue("@personID", personID);
                                update_Satisfied_Field.Parameters.AddWithValue("@projectID", projectID);
                                update_Satisfied_Field.Parameters.AddWithValue("@Satisfied", satisfied);
                                update_Satisfied_Field.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            //We create a new "Attractions" table because the last one goes on more than one hour.
                            //Before we get the two foreign key to fill the new table 
                            SqlCommand get_IDPeople_IDProjects = new SqlCommand("SELECT ID_People, ID_Projects FROM People, Projects WHERE ([personID] = @personID AND [projectID] = @projectID)", cnn);
                            get_IDPeople_IDProjects.Parameters.AddWithValue("@personID", personID);
                            get_IDPeople_IDProjects.Parameters.AddWithValue("@projectID", projectID);
                            SqlDataReader read_Foreign_Key = find_Attractions_table.ExecuteReader();
                            while (read_Foreign_Key.Read())
                            {
                                id_people = (int)read_Foreign_Key[0];
                                id_project = (int)read_Foreign_Key[1];
                            }
                            SqlCommand update_Satisfied_Field = new SqlCommand("INSERT INTO dbo.Attractions (DateUTC, Satisfied, Attention_time, ID_People, ID_Projects)" +
                                                                               "VALUES (@dateUTC, @satisfied, @ancestry, @age)"
                    }
                    }


                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                cnn.Close();
            }

        }


        /// <summary>
        /// This is a C# version of EyeFace Standard API example on how to process a videostream.
        /// </summary>
        
        /*public static void efEyeFaceStandardExample()
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

            VideoCapture capture = new VideoCapture();                                          //create a camera capture. Work with last EmguCV version 3.2
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

                /// We will create a JSON object and fill it with the data we need
                /// Before we need to verify that all datas are set before stocking them
                /// We free the image taken when we are done with it. 
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
                        //Save data into the JSON
                        Console.WriteLine("Data in the JSON \n");
                        person.WriteStartObject();

                        person.WritePropertyName("PersonID");
                        person.WriteValue(trackInfoArray.track_info[i].person_id);

                        person.WritePropertyName("Gender");
                        //person.WriteValue(attributesArray.face_attributes[i].gender.ToString());
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.gender.ToString());

                        person.WritePropertyName("Age");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.age.value);

                        person.WritePropertyName("Emotion");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.emotion.ToString());

                        person.WritePropertyName("Ancestry");
                        person.WriteValue(trackInfoArray.track_info[i].face_attributes.ancestry.ToString());

                        person.WritePropertyName("Attention_time");
                        person.WriteValue(trackInfoArray.track_info[i].attention_time);

                        person.WriteEndObject();

                        JObject o = (JObject)person.Token;
                        Console.WriteLine(o.ToString());

                        // free the image
                        efCsSDK.erImageFree(ref image);

                        saveDataIntoEyeFaceDB(o);
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
        }*/
    }
}
