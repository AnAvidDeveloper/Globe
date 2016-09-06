using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Printing;
using System.Windows.Shapes;

using Microsoft.Win32;

namespace Globe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Two times pi
        /// </summary>
        private const double twoPI = Math.PI * 2;

        /// <summary>
        /// Half of pi
        /// </summary>
        private const double halfPI = Math.PI / 2;

        // The main object model group.
        private Model3DGroup MainModel3Dgroup = new Model3DGroup();

        /// <summary>
        /// The camera
        /// </summary>
        private PerspectiveCamera TheCamera;

        // The camera's current location.
        private double CameraPhi = Math.PI / 6.0;       // 30 degrees
        private double CameraTheta = Math.PI / 6.0;     // 30 degrees
        private double CameraDistance = 3.0;            // 3 units away from globe

        // The change in CameraPhi when you press the up and down arrows.
        private const double CameraDeltaPhi = 0.1;

        /// <summary>
        /// The change in CameraTheta when you press the left and right arrows. 
        /// </summary>
        private const double CameraDeltaTheta = 0.1;

        /// <summary>
        /// The change in CameraDistance when you press + or -. 
        /// </summary>
        private const double CameraDeltaDistance = 0.1;

        /// <summary>
        /// Model of the globe
        /// </summary>
        private GeometryModel3D GlobeModel = null;

        /// <summary>
        /// Timer for animation (spinning, clouds, etc.)
        /// </summary>
        private System.Windows.Threading.DispatcherTimer TheTimer = null;

        /// <summary>
        /// Is spinning the globe enabled?
        /// </summary>
        private bool SpinEnabled = false;

        /// <summary>
        /// When the window is loaded, creates the 3D scene and initializes the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Give the camera its initial position.
            TheCamera = new PerspectiveCamera();
            TheCamera.FieldOfView = 60;
            MainViewport.Camera = TheCamera;
            PositionCamera();

            // Add lights to the main model group.
            AddLights(MainModel3Dgroup);

            // Create the model of the globe.
            GlobeModel = CreateGlobeModel();
            MainModel3Dgroup.Children.Add(GlobeModel);

            // Add the group of models to a ModelVisual3D.
            ModelVisual3D model_visual = new ModelVisual3D();
            model_visual.Content = MainModel3Dgroup;

            // Display the main visual to the viewportt.
            MainViewport.Children.Add(model_visual);


            TheTimer = new System.Windows.Threading.DispatcherTimer();
            TheTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            TheTimer.Tick += TheTimer_Tick;
            TheTimer.Start();
        }

        /// <summary>
        /// Performs animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TheTimer_Tick(object sender, EventArgs e)
        {
            if (SpinEnabled)
            {
                CameraTheta += CameraDeltaTheta;
                PositionCamera();
            }
        }


        /// <summary>
        /// Positions the camera relative to the globe.
        /// </summary>
        private void PositionCamera()
        {
            // Calculate the camera's position in Cartesian coordinates,
            // based upon the camera's distance from the globe and the
            // rotation angles (CameraPhi and CameraThere).
            double y = CameraDistance * Math.Sin(CameraPhi);
            double hyp = CameraDistance * Math.Cos(CameraPhi);
            double x = hyp * Math.Cos(CameraTheta);
            double z = hyp * Math.Sin(CameraTheta);
            TheCamera.Position = new Point3D(x, y, z);

            // Look toward the origin.
            TheCamera.LookDirection = new Vector3D(-x, -y, -z);

            // Set the Up direction.
            TheCamera.UpDirection = new Vector3D(0, 1, 0);
        }

        
        /// <summary>
        /// Adds lighting to the specified model group.
        /// </summary>
        private void AddLights(Model3DGroup modelGroup)
        {
            AmbientLight ambientLight = new AmbientLight(Colors.DarkGray);
            DirectionalLight directionalLight =
                new DirectionalLight(Colors.Gray, new Vector3D(-1.0, -3.0, -2.0));
            modelGroup.Children.Add(ambientLight);
            modelGroup.Children.Add(directionalLight);
        }


        /// <summary>
        /// Creates a globe model.
        /// </summary>
        /// <param name="modelGroup"></param>
        private GeometryModel3D CreateGlobeModel()
        {
            // Make spheres centered at (0, 0, 0).
            MeshGeometry3D mesh1 = new MeshGeometry3D();
            AddSmoothSphere(mesh1, new Point3D(0, 0, 0), 1, 20, 40);
            SolidColorBrush brush1 = Brushes.Blue;  // default to blue until image selected or generate option chosen
            DiffuseMaterial material1 = new DiffuseMaterial(brush1);
            GeometryModel3D globeModel = new GeometryModel3D(mesh1, material1);
            return globeModel;
        }

        // Add a triangle to the indicated mesh.
        // Reuse points so triangles share normals.
        // Add texture coordinates only for points being added, not those being reused.
        private void AddSmoothTexturedTriangle(MeshGeometry3D mesh, Dictionary<Point3D, int> dict, 
            Point3D point1, Point3D point2, Point3D point3, Point uv1, Point uv2, Point uv3)
        {
            int index1, index2, index3;

            // Find or create the points.
            if (dict.ContainsKey(point1))
                index1 = dict[point1];
            else
            {
                index1 = mesh.Positions.Count;
                mesh.Positions.Add(point1);
                mesh.TextureCoordinates.Add(uv1);
                dict.Add(point1, index1);
            }

            if (dict.ContainsKey(point2))
                index2 = dict[point2];
            else
            {
                index2 = mesh.Positions.Count;
                mesh.Positions.Add(point2);
                mesh.TextureCoordinates.Add(uv2);
                dict.Add(point2, index2);
            }

            if (dict.ContainsKey(point3))
                index3 = dict[point3];
            else
            {
                index3 = mesh.Positions.Count;
                mesh.Positions.Add(point3);
                mesh.TextureCoordinates.Add(uv3);
                dict.Add(point3, index3);
            }

            // If two or more of the points are
            // the same, it's not a triangle.
            if ((index1 == index2) ||
                (index2 == index3) ||
                (index3 == index1))
                return;

            // Create the triangle.
            mesh.TriangleIndices.Add(index1);
            mesh.TriangleIndices.Add(index2);
            mesh.TriangleIndices.Add(index3);
        }

        // Add a triangle to the indicated mesh.
        // Add texture coordinates.
        private void AddTexturedTriangle(MeshGeometry3D mesh, Dictionary<Point3D, int> dict,
            Point3D point1, Point3D point2, Point3D point3, Point uv1, Point uv2, Point uv3)
        {
            int index1, index2, index3;

            // Create the points.
            index1 = mesh.Positions.Count;
            mesh.Positions.Add(point1);
            mesh.TextureCoordinates.Add(uv1);
            if (!dict.ContainsKey(point1))
                dict.Add(point1, index1);

            index2 = mesh.Positions.Count;
            mesh.Positions.Add(point2);
            mesh.TextureCoordinates.Add(uv2);
            if (!dict.ContainsKey(point2))
                dict.Add(point2, index2);

            index3 = mesh.Positions.Count;
            mesh.Positions.Add(point3);
            mesh.TextureCoordinates.Add(uv3);
            if (!dict.ContainsKey(point3))
                dict.Add(point3, index3);

            // If two or more of the points are
            // the same, it's not a triangle.
            if ((point1 == point2) ||
                (point2 == point3) ||
                (point3 == point1))
                return;

            // Create the triangle.
            mesh.TriangleIndices.Add(index1);
            mesh.TriangleIndices.Add(index2);
            mesh.TriangleIndices.Add(index3);
        }


        /// <summary>
        /// Returns the UV (texture coordinates) for a point on a sphere.
        /// </summary>
        /// <param name="center">center of the sphere</param>
        /// <param name="radius">radius of the sphere</param>
        /// <param name="point">point on the sphere who's UV we seek</param>
        /// <returns></returns>
        private Point GetSphereUV(Point3D center, double radius, Point3D point)
        {
            Point3D effectivePoint = (Point3D)(point - center);
            double lat = Math.Acos(effectivePoint.Y / radius); //theta
            double lon = Math.Atan2(effectivePoint.Z, effectivePoint.X); //phi
            if (lon < 0)
                lon += twoPI;
            double v = lat / Math.PI;
            double u = (twoPI - (lon / twoPI)) / twoPI;
            return new Point(u, v);
        }

        /// <summary>
        /// Generates a mesh for a smooth sphere.  "Smoothness" is achieved by sharing vertices so that
        /// the 3D system generates vertex normals rather than relying upon face/triangle normals.
        /// </summary>
        /// <param name="mesh">mesh to add the sphere to</param>
        /// <param name="center">center point of the sphere</param>
        /// <param name="radius">radius of the sphere</param>
        /// <param name="numPhi">numer of stacks along phi (y)</param>
        /// <param name="numTheta">number of slices along theta (x-z)</param>
        private void AddSmoothSphere(MeshGeometry3D mesh, Point3D center, double radius, int numPhi, int numTheta)
        {
            // Make a dictionary to track the sphere's points.
            Dictionary<Point3D, int> dict = new Dictionary<Point3D, int>();

            double phi0, theta0;
            double dphi = Math.PI / numPhi;
            double dtheta = 2 * Math.PI / numTheta;

            phi0 = 0;
            double y0 = radius * Math.Cos(phi0);
            double r0 = radius * Math.Sin(phi0);
            for (int i = 0; i < numPhi; i++)
            {
                double phi1 = phi0 + dphi;
                double y1 = radius * Math.Cos(phi1);
                double r1 = radius * Math.Sin(phi1);

                // Point ptAB has phi value A and theta value B.
                // For example, pt01 has phi = phi0 and theta = theta1.
                // Find the points with theta = theta0.
                theta0 = 0;
                Point3D pt00 = new Point3D(
                    center.X + r0 * Math.Cos(theta0),
                    center.Y + y0,
                    center.Z + r0 * Math.Sin(theta0));
                Point3D pt10 = new Point3D(
                    center.X + r1 * Math.Cos(theta0),
                    center.Y + y1,
                    center.Z + r1 * Math.Sin(theta0));

                // Calculate UV for the two points we just found.
                Point uv00 = GetSphereUV(center, radius, pt00);
                Point uv10 = GetSphereUV(center, radius, pt10);

                for (int j = 0; j < numTheta; j++)
                {
                    // Find the points with theta = theta1.
                    double theta1 = theta0 + dtheta;
                    Point3D pt01 = new Point3D(
                        center.X + r0 * Math.Cos(theta1),
                        center.Y + y0,
                        center.Z + r0 * Math.Sin(theta1));
                    Point3D pt11 = new Point3D(
                        center.X + r1 * Math.Cos(theta1),
                        center.Y + y1,
                        center.Z + r1 * Math.Sin(theta1));

                    // Calculate UV for the two points we just found.
                    Point uv01 = GetSphereUV(center, radius, pt01);
                    Point uv11 = GetSphereUV(center, radius, pt11);

                    // Create the triangles, with texture coordinates.
                    AddSmoothTexturedTriangle(mesh, dict, pt00, pt11, pt10, uv00, uv11, uv10);
                    AddSmoothTexturedTriangle(mesh, dict, pt00, pt01, pt11, uv00, uv01, uv11);

                    // Move to the next value of theta.
                    theta0 = theta1;
                    pt00 = pt01;
                    pt10 = pt11;
                }

                // Move to the next value of phi.
                phi0 = phi1;
                y0 = y1;
                r0 = r1;
            }
        }


        /// <summary>
        /// Based upon keys pressed, adjusts the camera's position. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    CameraPhi += CameraDeltaPhi;
                    if (CameraPhi > Math.PI / 2.0) CameraPhi = Math.PI / 2.0;
                    break;
                case Key.Down:
                    CameraPhi -= CameraDeltaPhi;
                    if (CameraPhi < -Math.PI / 2.0) CameraPhi = -Math.PI / 2.0;
                    break;
                case Key.Left:
                    CameraTheta += CameraDeltaTheta;
                    break;
                case Key.Right:
                    CameraTheta -= CameraDeltaTheta;
                    break;
                case Key.Add:
                case Key.OemPlus:
                case Key.PageDown:
                    CameraDistance -= CameraDeltaDistance;
                    if (CameraDistance < CameraDeltaDistance) CameraDistance = CameraDeltaDistance;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                case Key.PageUp:
                    CameraDistance += CameraDeltaDistance;
                    break;
                case Key.Home:
                    // Reset camera to default orientation
                    CameraPhi = Math.PI / 6.0;       // 30 degrees
                    CameraTheta = Math.PI / 6.0;     // 30 degrees
                    CameraDistance = 3.0;            // 3 units away from the sphere
                    break;
            }

            // Update the camera's position.
            PositionCamera();
        }

        /// <summary>
        /// Applies the specified image to the globe.
        /// </summary>
        /// <param name="image"></param>
        private void ApplyImageToGlobe(BitmapImage image)
        {
            var imgBrush = new ImageBrush(image);            
            imgBrush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            imgBrush.TileMode = TileMode.Tile;  // Helps prevent seams, even if we really don't intend to tile

            if (GlobeModel == null)
                return;

            var matl = GlobeModel.Material as DiffuseMaterial;
            matl.Brush = imgBrush;                       
        }


        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var img = new BitmapImage(new Uri(dlg.FileName));
                    ApplyImageToGlobe(img);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Loading Image", 
                        MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG format|*.png|JPEG format|*.jpg;*.jpeg";
            if (dlg.ShowDialog() != true) return;


            // Render to a bitmap.
            RenderTargetBitmap bmp = new RenderTargetBitmap(
            (int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);            
            bmp.Render(MainViewport);

            // Save bitmap to file.
            BitmapEncoder encoder = null;
            switch (dlg.FilterIndex )
            {
                case 0:
                    encoder = new PngBitmapEncoder();
                    break;
                case 1:
                    encoder = new JpegBitmapEncoder();
                    break;
            }
            if (encoder == null)
            {
                MessageBox.Show("Invalid format selection.", "Could Not Export", 
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                return;
            }
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (Stream stm = File.Create(dlg.FileName))
            {
                encoder.Save(stm);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                dlg.PrintVisual(MainViewport, "Globe");
            }

        }


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewSpin_Click(object sender, RoutedEventArgs e)
        {
            mnuViewSpin.IsChecked = !mnuViewSpin.IsChecked;
            SpinEnabled = mnuViewSpin.IsChecked;
        }

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow help = new HelpWindow();
            help.Show();
        }

        //private void ViewSun_Click(object sender, RoutedEventArgs e)
        //{
        //    mnuViewSun.IsChecked = !mnuViewSun.IsChecked;
        //}
    }
}
