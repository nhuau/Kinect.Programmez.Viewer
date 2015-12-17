using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Kinect.Programmez.Viewer
{
    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private const int RedIndex = 2;
        private const int GreenIndex = 1;
        private const int BlueIndex = 0;
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private int _maxDepth;
        private int _minDepth;
        private byte[] _depthFrame32;
        private short[] _depthPixelData;

        /// <summary>
        ///   Capteur kinect utilisé.
        /// </summary>
        private KinectSensor _sensor;

        private byte[] _videoPixelData;

        private readonly ColorImageFormat _colorFormat = ColorImageFormat.RgbResolution640x480Fps30;
        private readonly DepthImageFormat _depthFormat = DepthImageFormat.Resolution640x480Fps30;

        #endregion



        #region Properties

        /// <summary>
        ///   Image résultante de la traitement du flux vidéo. Il est préférable pour des questions 
        /// de performance de privilégier l'utilisation d'une WriteableBitmap, mieux adapté à une 
        /// réécriture fréquente, plutôt que de constamment recréer un object de type BitmapSource.
        /// </summary>
        public WriteableBitmap Video { get; set; }
        /// <summary>
        ///   Image résultante de la traitement du flux de profondeur. Il est préférable pour des questions 
        /// de performance de privilégier l'utilisation d'une WriteableBitmap, mieux adapté à une 
        /// réécriture fréquente, plutôt que de constamment recréer un object de type BitmapSource.
        /// </summary>
        public WriteableBitmap Depth { get; set; }

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            InitSensor();
            DataContext = this;
        }
        #endregion



        #region Methods


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                 Etape n°0 : Recuperation du capteur                                                                        ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   Récupère et paramètre le premier capteur disponible.
        /// </summary>
        private void InitSensor()
        {
            // Récupération du premier capteur connecté
            _sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (_sensor == null)
            {
                MessageBox.Show("Aucun capteur Kinect n'a été détecté.", "Erreur", MessageBoxButton.OK);
                return;
            }

            // Activation des flux
            _sensor.ColorStream.Enable(_colorFormat);
            _sensor.DepthStream.Enable(_depthFormat);
            _sensor.SkeletonStream.Enable();

            // Branchements aux évènements du capteur
            _sensor.ColorFrameReady += OnColorFrameReady; // Vidéo mise à jour
            _sensor.DepthFrameReady += OnDepthFrameReady; // Image de profondeur mise à jour            
            _sensor.SkeletonFrameReady += OnSkeletonFrameReady; // Squelettes mis à jour
            _sensor.AllFramesReady += OnAllFramesReady; // Toute les frames ont été mise à jour

            // Démarre le capteur
            if (!_sensor.IsRunning) _sensor.Start();
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                 Etape n°1 : Transformation de la vidéo                                                                     ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        ///   Gestionnaire de l'évènement ColorFrameReady du capteur Kinect.
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="args"> </param>
        private void OnColorFrameReady(object sender, ColorImageFrameReadyEventArgs args)
        {
            using (var frame = args.OpenColorImageFrame())
            {
                if (frame == null)
                {
                    // Peut survenir ponctuellement notamment lorsque la Kinect est débranchée ou arrétée.
                    return;
                }

                if (_videoPixelData == null || Video == null)
                {
                    // Initialisation du buffer intermédiaire ainsi que de la WriteableBitmap.
                    _videoPixelData = new byte[frame.PixelDataLength];
                    Video = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
                }

                // Récupération des données de la frame
                frame.CopyPixelDataTo(_videoPixelData);
                // Réécriture le l'image
                Video.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height),
                                  _videoPixelData,
                                  frame.Width * Bgr32BytesPerPixel,
                                  0);
            }
            RaiseVideoUpdated();
        }

        private void RaiseVideoUpdated()
        {
            imVideo.Source = Video;
        }

        private int compt;
        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs args)
        {
            compt++;
            if (compt > 300)
            {
                compt = 0;
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                 Etape n°2 : Transformation de la Depth                                                                     ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gestionnaire de l'évènement DepthFrameReadyy du capteur Kinect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs args)
        {
            using (var frame = args.OpenDepthImageFrame())
            {
                if (frame == null)
                {
                    return;
                }

                if (_depthFrame32 == null || _depthPixelData == null || Depth == null)
                {
                    // initialisation au besoin des différents éléments
                    Depth = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                                                PixelFormats.Bgr32,
                                                null);
                    _depthFrame32 = new byte[frame.Width * frame.Height * Bgr32BytesPerPixel];
                    _depthPixelData = new short[frame.PixelDataLength];
                    _minDepth = _sensor.DepthStream.TooNearDepth;
                    _maxDepth = _sensor.DepthStream.TooFarDepth;
                }

                // Récupération des pixels de la frames
                frame.CopyPixelDataTo(_depthPixelData);

                // Conversion des pixels 16 bits en un tableau d'octets
                _depthFrame32 = ConvertDepthFrame(_depthPixelData);

                Depth.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    _depthFrame32,
                    frame.Width * Bgr32BytesPerPixel,
                    0);
            }

            // Levée de l'évènement notifiant la mise à jour de l'image de profondeur.
            RaiseDepthUpdated();
        }

        /// <summary>
        /// Converti l'image 16 bits fournie par le capteur en une image 32 bits au format RGB.
        /// </summary>
        /// <param name="depthFrame16"></param>
        /// <returns></returns>
        protected byte[] ConvertDepthFrame(short[] depthFrame16)
        {
            for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < _depthFrame32.Length; i16++, i32 += 4)
            {
                // Récupération de l'indice du joueur
                int player = depthFrame16[i16] & DepthImageFrame.PlayerIndexBitmask;
                // Récupération de la valeur de la profondeur
                int realDepth = depthFrame16[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // Récupération des 8 bits de poid fort
                var intensity = (byte)(~(realDepth >> 4));


                if (realDepth <= _minDepth)
                {
                    // Affiche en blanc les pixels trop proche du capteur
                    _depthFrame32[i32 + RedIndex] = 255;
                    _depthFrame32[i32 + GreenIndex] = 255;
                    _depthFrame32[i32 + BlueIndex] = 255;
                    continue;
                }
                if (realDepth >= _maxDepth)
                {
                    // Affiche en noir les pixels trop éloignés du capteur
                    _depthFrame32[i32 + RedIndex] = 0;
                    _depthFrame32[i32 + GreenIndex] = 0;
                    _depthFrame32[i32 + BlueIndex] = 0;
                    continue;
                }

                // Adaptation de la couleur en fonction de l'indice du joueur
                switch (player)
                {
                    case 0: // Aucun joueur
                        // Nuances de gris proportionnelles à la profondeur
                        _depthFrame32[i32 + RedIndex] = (byte)(intensity / 2);
                        _depthFrame32[i32 + GreenIndex] = (byte)(intensity / 2);
                        _depthFrame32[i32 + BlueIndex] = (byte)(intensity / 2);
                        break;
                    default: // Tout les joueurs
                        // Nuances de rouge proportionnelles à la profondeur
                        _depthFrame32[i32 + RedIndex] = intensity;
                        _depthFrame32[i32 + GreenIndex] = 0;
                        _depthFrame32[i32 + BlueIndex] = 0;
                        break;
                }
            }
            return _depthFrame32;
        }

        private void RaiseDepthUpdated()
        {
            imDepth.Source = Depth;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                 Etape n°3 : Affichage des squelettes                                                                       ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gestionnaire de l'évenement AllFramesReady du capteur Kinect. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAllFramesReady(object sender, AllFramesReadyEventArgs args)
        {
            Skeleton[] skeletons;
            using (var skframe = args.OpenSkeletonFrame())
            {
                if (skframe == null) return;

                skeletons = new Skeleton[skframe.SkeletonArrayLength];
                skframe.CopySkeletonDataTo(skeletons);
            }

            using (var depthframe = args.OpenDepthImageFrame())
            {
                if (depthframe == null) return;

                _depthSkeletons.Clear();
                _colorSkeletons.Clear();
                foreach (var skeleton in skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked)
                                                  .OrderBy(s => s.TrackingId))
                {

                    var depthSk = new Dictionary<JointType, DepthImagePoint>();
                    var videoSk = new Dictionary<JointType, ColorImagePoint>();
                    foreach (Joint joint in skeleton.Joints)
                    {
                        // Recupere le pixel de l'image de profondeur correspondant à la jointure
                        depthSk[joint.JointType] = depthframe.MapFromSkeletonPoint(joint.Position);
                        // Recupere le pixel de l'image video correspondant au pixel de l'image de profondeur
                        videoSk[joint.JointType] = depthframe.MapToColorImagePoint(
                                                            depthSk[joint.JointType].X,
                                                            depthSk[joint.JointType].Y, _colorFormat);
                    }
                    _depthSkeletons.Add(depthSk);
                    _colorSkeletons.Add(videoSk);
                }
            }

            // Levée de l'évènement notifiant la mise à jour des squelettes.
            RaiseSkeletonsUpdated();
        }

        private void RaiseSkeletonsUpdated()
        {
            //depthSkeletonViewer.DepthSkeletons = DepthSkeletons;
            depthSkeletonViewer.SkeletonPointToUiElement(_depthSkeletons);
            //videoSkeletonViewer.ColorSkeletons = ColorSkeletons;
            videoSkeletonViewer.SkeletonPointToUiElement(_colorSkeletons);
        }

        /// <summary>
        /// Liste des squelettes mappés sur l'image de profondeur.
        /// </summary>
        private readonly List<Dictionary<JointType, DepthImagePoint>> _depthSkeletons = new List<Dictionary<JointType, DepthImagePoint>>();
        /// <summary>
        /// Liste des squelettes mappés sur l'image vidéo.
        /// </summary>
        private readonly List<Dictionary<JointType, ColorImagePoint>> _colorSkeletons = new List<Dictionary<JointType, ColorImagePoint>>();



        /// <summary>
        ///   Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender"> The source of the event. </param>
        /// <param name="e"> The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the event data. </param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_sensor != null && _sensor.IsRunning)
            {
                _sensor.Stop();
            }
        }

        #endregion
    }
}
