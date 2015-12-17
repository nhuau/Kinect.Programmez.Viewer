using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Kinect.Programmez.Viewer
{
    /// <summary>
    ///   Interaction logic for SkeletonViewer.xaml
    /// </summary>
    public partial class VideoSkeletonViewer : UserControl
    {
        #region Dependency  Properties

        // Using a DependencyProperty as the backing store for DepthmainCanvas.Children.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorSkeletonsProperty =
            DependencyProperty.Register("ColorSkeletons", typeof (List<Dictionary<JointType, ColorImagePoint>>),
                                        typeof (VideoSkeletonViewer), new UIPropertyMetadata(null, OnDepthSkeletonsChanged));

        public List<Dictionary<JointType, ColorImagePoint>> ColorSkeletons
        {
            get { return (List<Dictionary<JointType, ColorImagePoint>>) GetValue(ColorSkeletonsProperty); }
            set { SetValue(ColorSkeletonsProperty, value); }
        }

        private static void OnDepthSkeletonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VideoSkeletonViewer sender;
            if ((sender = d as VideoSkeletonViewer) == null)
            {
                return;
            }
            sender.SkeletonPointToUiElement(sender.ColorSkeletons);
        }

        #endregion

        #region Constructor

        /// <summary>
        ///   Initializes a new instance of the <see cref="VideoSkeletonViewer" /> class.
        /// </summary>
        public VideoSkeletonViewer()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Pinceaux utilises pour dessiner les squelettes. - 0: Pour Mainplayer - 1: Pour AssistantPlayer - 2: Pour Players ne pouvant intervenir
        /// </summary>
        protected Brush brushes =
            new SolidColorBrush(Color.FromRgb(0, 0, 255));

        /// <summary>
        ///   Dictionnaire des couleurs à assigner à chacune des jointures.
        /// </summary>
        protected Brush jointColors = new SolidColorBrush(Color.FromRgb(181, 165, 213));

        //public List<UIElement> Skeletons { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///   Dessine les jointures du squelette.
        /// </summary>
        public void SkeletonPointToUiElement(List<Dictionary<JointType, ColorImagePoint>> colorSkeletons)
        {
            mainCanvas.Children.Clear();
            foreach (Dictionary<JointType, ColorImagePoint> adjusted in colorSkeletons)
            {
                mainCanvas.Children.Add(getBodySegment(adjusted, brushes, JointType.HipCenter, JointType.Spine,
                                                       JointType.ShoulderCenter, JointType.Head));
                mainCanvas.Children.Add(getBodySegment(adjusted, brushes, JointType.ShoulderCenter,
                                                       JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft,
                                                       JointType.HandLeft));
                mainCanvas.Children.Add(getBodySegment(adjusted, brushes, JointType.ShoulderCenter,
                                                       JointType.ShoulderRight, JointType.ElbowRight,
                                                       JointType.WristRight, JointType.HandRight));
                mainCanvas.Children.Add(getBodySegment(adjusted, brushes, JointType.HipCenter, JointType.HipLeft,
                                                       JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                mainCanvas.Children.Add(getBodySegment(adjusted, brushes, JointType.HipCenter, JointType.HipRight,
                                                       JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));

                // Draw joints
                foreach (KeyValuePair<JointType, ColorImagePoint> elt in adjusted)
                {
                    Ellipse ellipse;
                    Line jointLine;

                    switch (elt.Key)
                    {
                        case JointType.HandLeft:
                        case JointType.HandRight:

                            #region Dessine deux ellipses pour les mains

                            ellipse = new Ellipse
                                          {
                                              Stroke = Brushes.Black,
                                              Fill = jointColors,
                                              HorizontalAlignment = HorizontalAlignment.Left,
                                              VerticalAlignment = VerticalAlignment.Center,
                                              Width = 30,
                                              Height = 30
                                          };


                            double left = elt.Value.X - 15;
                            double top = elt.Value.Y - 15;

                            ellipse.Margin = new Thickness(left, top, 0, 0);

                            mainCanvas.Children.Add(ellipse);

                            #endregion

                            break;
                        case JointType.Head:

                            #region Dessine un petits carre + un cercle pour la tete

                            jointLine = new Line();
                            jointLine.X1 = elt.Value.X - 3;
                            jointLine.X2 = jointLine.X1 + 6;
                            jointLine.Y1 = jointLine.Y2 = elt.Value.Y;
                            jointLine.Stroke = jointColors;

                            jointLine.StrokeThickness = 6;

                            mainCanvas.Children.Add(jointLine);

                            ellipse = new Ellipse
                                          {
                                              Stroke = brushes,
                                              HorizontalAlignment = HorizontalAlignment.Left,
                                              VerticalAlignment = VerticalAlignment.Center,
                                              Width = 80,
                                              Height = 80,
                                              Margin = new Thickness(elt.Value.X - 40, elt.Value.Y - 40, 0, 0)
                                          };


                            mainCanvas.Children.Add(ellipse);

                            #endregion

                            break;
                        default:

                            #region Dessine des petits carres pour les autre jointures

                            jointLine = new Line();
                            jointLine.X1 = elt.Value.X - 3;
                            jointLine.X2 = jointLine.X1 + 6;
                            jointLine.Y1 = jointLine.Y2 = elt.Value.Y;
                            jointLine.Stroke = jointColors;

                            jointLine.StrokeThickness = 6;

                            mainCanvas.Children.Add(jointLine);

                            #endregion

                            break;
                    }
                }
            }
        }

        /// <summary>
        ///   Gets the body segment.
        /// </summary>
        /// <param name="skeletons"> The skeleton point. </param>
        /// <param name="brush"> The brush. </param>
        /// <param name="ids"> The ids. </param>
        /// <returns> </returns>
        private Polyline getBodySegment(Dictionary<JointType, ColorImagePoint> skeletons, Brush brush,
                                        params JointType[] ids)
        {
            var points = new PointCollection(ids.Length);
            foreach (JointType t in ids)
            {
                points.Add(new Point(skeletons[t].X, skeletons[t].Y));
            }

            return new Polyline {Points = points, Stroke = brush, StrokeThickness = 5};
        }


        /*
        /// <summary>
        /// Matches the size of the given skeleton with the with screen.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public static Dictionary<JointType, DepthImagePoint> MatchWithScreenSize(Dictionary<JointType, DepthImagePoint> skeleton, int width, int height)
        {
            foreach (DepthImagePoint point in skeleton.Values)
            {
                point.X *= width;
                point.Y *= height;
            }
            return skeleton;
        }
        */

        #endregion
    }
}