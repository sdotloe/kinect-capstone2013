﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using System.Windows.Controls;
    using System.Timers;//
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.ComponentModel;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //----- My Variables
        private double hipMin = 2;
        private double hipMax = -2;
        
        private double kneeMin = 2;
        private double kneeMax = -2;

        private Joint hipR; 
        private Joint kneeR;
        private Joint head;
        private Joint shoulderC;
        private Joint shoulderL;
        private Joint shoulderR;
        private Joint handR;
        private Joint handL;
        private Duration duration;

        DispatcherTimer dt = new DispatcherTimer();
        Stopwatch stopWatch = new Stopwatch();
        string currentTime = string.Empty;



        //Number of Squats completed
        private int repCount = 0;

        //True if mid position is reached while exrecising
        private bool reachedMid = false;

        //True if Kinect is tracking & User has initiated exercise
        private bool Exercising = false;
        //-----


        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen trackedBonePen = new Pen(Brushes.Green, 6); //changed from readonly

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ///////////////////////////////////////////////////////
            dt.Tick += new EventHandler(dt_Tick);
            dt.Interval = new TimeSpan(0, 0, 0, 0, 1);
            ////////////////////////////////////////////////////////
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);
                ClockTextBlock.Text = "Duration: " + currentTime;

            }
        }

        private void StartButton_Click()//object sender, RoutedEventArgs e
        {
           // if (startTimer == true)
          //  {
                stopWatch.Start();
                dt.Start();
           // }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
 //               this.statusBarText.Text = Properties.Resources.NoKinectReady;///////////////////////////////////////
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }


        private void ExerciseID(Skeleton skeleton)
        {
            if (skeleton.Joints[JointType.ShoulderRight].Position.Y - skeleton.Joints[JointType.WristRight].Position.Y < 0) // and if standing -> each joint is above the rest
            {
              //  System.Console.WriteLine("HAND AND SHOULDER: "+((skeleton.Joints[JointType.ShoulderRight].Position.Y - skeleton.Joints[JointType.WristRight].Position.Y)));

            }
        }

        //Squat Tracker
        private void StartStopExercise(Skeleton skeleton)
        {
            //Raise Right Hand above Head to Begin exercise
            if (Exercising == false && (skeleton.Joints[JointType.HandRight].Position.Y - skeleton.Joints[JointType.Head].Position.Y > 0.1))
            {
               // System.Console.WriteLine("Start Exercise ");
              //  hipR = skeleton.Joints[JointType.HipRight]; //maybe change to HipRight
                StartButton_Click();
                head = skeleton.Joints[JointType.Head];
                kneeR = skeleton.Joints[JointType.KneeRight];
                Exercising = true;
                System.Console.WriteLine("Begin Squats");
            }

            //Raise Left Hand above Head to End exercise
            if (Exercising == true && (skeleton.Joints[JointType.HandLeft].Position.Y - skeleton.Joints[JointType.Head].Position.Y > 0.1 ))
            {
                Exercising = false;
                System.Console.WriteLine("Final Rep Count = " + repCount);
                repCount = 0;
               // System.Console.WriteLine("Hip Max = " + hipMax + " Hip Min = " + hipMin);
                //System.Console.WriteLine("Knee Max = " + kneeMax + " Knee Min = " + kneeMin);
                //System.Console.WriteLine("Hip Depth = " + skeleton.Joints[JointType.HipCenter].Position.Z);
                this.Close();
                return;
            }


                if (Exercising)
                {
                    
                    hipR = skeleton.Joints[JointType.HipRight]; 
                //    Joint kneeR = skeleton.Joints[JointType.KneeRight];

                 /*
                    //------------------------Delete----------------
                    if (hipR.Position.Y > hipMax)
                    {
                        hipMax = hipR.Position.Y;
                    }

                    if (hipR.Position.Y < hipMin)
                    {
                        hipMin = hipR.Position.Y;
                    }

                    if (kneeR.Position.Y > kneeMax)
                    {
                        kneeMax = kneeR.Position.Y;
                    }

                    if (kneeR.Position.Y < kneeMin)
                    {
                        kneeMin = kneeR.Position.Y;
                    }
                    //-------------------------------Delete----------------------------
                    */

                    //Mid Position Reached
                    if (hipR.Position.Y - kneeR.Position.Y < 0.03) //Hip is lower than the knee (0.03m = 1.8in)
                    {
                      // System.Console.WriteLine("Hip - Knee = " + (hipR.Position.Y - kneeR.Position.Y));
                       reachedMid = true;
                       trackedBonePen = new Pen(Brushes.LemonChiffon, 6); //LemonChiffon skeleton indicates Mid position reached 
                    }

                    //Temporary Start Position condition
                    //if (reachedMid && (hipR.Position.Y > kneeR.Position.Y) )
                    if (reachedMid && (head.Position.Y - skeleton.Joints[JointType.Head].Position.Y < 0.1 )) //(0.1m = 3.9in)
                    {

                        trackedBonePen = new Pen(Brushes.Green, 6); //Green skeleton indicates that Start position
                        reachedMid = false;
                        repCount++;
                        statusBarText.Text = "Rep: " + repCount;
                        System.Console.WriteLine("Rep Count = " + repCount);
                    }

                }
            }
        
    

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                           
                            //ID Squats
                            ExerciseID(skel);
                            //Track Squats
                            StartStopExercise(skel);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            //System.Console.WriteLine("This is when PositionOnly Occurs");  // FOR DEBUGGING (Delete this line)
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }
    

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
        /*
        private void duration(object sender, RoutedEventArgs e)
        {
          // Timer timer = new Timer();
           // timer.Start();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var label = sender as Label;
            label.Content = "Duration: " + stopwatch.Elapsed;
        }
        */
    
 
        /*
            public event PropertyChangedEventHandler PropertyChanged;
            public string TimeElapsed { get; set; }

            private DispatcherTimer timer;
            private Stopwatch stopWatch;

            public void StartTimer()
            {
                timer = new DispatcherTimer();
                timer.Tick += dispatcherTimerTick_;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                stopWatch = new Stopwatch();
                stopWatch.Start();
                timer.Start();
                
            }



            private void dispatcherTimerTick_(object sender, EventArgs e)
            {
                TimeElapsed = "" + stopWatch.Elapsed.TotalMilliseconds; // Format as you wish
                var label = sender as Label;
                label.Content =""+ TimeElapsed;
                PropertyChanged(this, new PropertyChangedEventArgs("TimeElapsed"));
            }
        
        */
        //date sent to UI
        private void currentDate(object sender, RoutedEventArgs e)
        {
            var label = sender as Label;
            label.Content = "Date: " + DateTime.Now.ToShortDateString();
           // DateTime now = DateTime.Now;
       //     string date = now.GetDateTimeFormats('d')[0];
        //    return date;
        }


/*
        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }*/
    }
}