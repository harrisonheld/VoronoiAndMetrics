using System;

// for drawing and saving images
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace Voronoi
{
    class Program
    {
        // where to save the images to
        const string DIRECTORY = @"[SPECIFY A DIRECTORY HERE]";
        // where a font can be found to write text (.ttf files only)
        const string FONT_PATH = @"[SPECIFY THE PATH TO A .ttf FILE HERE]";

        // this random is used only for fuzziness, so we don't introduce noise to the site random
        static Random fuzzyRand = new Random();

        static void Main()
        {
            var metrics = new Func<int, int, int, int, double>[5]
            {
                FuzzyEuclidian,
                Euclidian,
                Minkowski,
                Manhattan,
                Canberra,
            };

            int seed = Environment.TickCount;

            foreach (var metric in metrics) // generate a voronoi diagram for each metric
            {
                string filePath = DIRECTORY + seed + "_" + metric.Method.Name + ".png";
                Voronoi(filePath, metric, seed);
                Console.WriteLine("Saved an image to {0}.", filePath);
            }
        }

        ///<summary>
        /// Creates a Voronoi diagram and saves it to the specified path.
        /// </summary>
        /// <param name="metric">A function that takes in 4 integers (x1, y1, x2, y2), and outputs a distance as a double.</param>
        /// <param name="drawSites">True if sites should be drawn as black dots. False if they should not be rendered.</param>
        static void Voronoi(string path, Func<int, int, int, int, double> metric, int seed = 0, int width = 500, int height = 500, int siteCount = 20, bool drawSites = true)
        {
            // a Random used for placing sites at random positions
            Random siteRand = new Random(seed);

            // the sites, as points
            Point[] sites = new Point[siteCount];
            // the colors that will correspond to each cell
            Color[] colors = new Color[siteCount];

            // generate sites and colors
            for (int i = 0; i < siteCount; i++)
            {
                // make a site with random coordinates
                int x = siteRand.Next(width);
                int y = siteRand.Next(height);
                sites[i] = new Point(x, y);

                // assign a random color to the site
                // I restrict the color to the following range because brighter colors look nicer
                int intensityMin = 128; // all RGB components will be >= this number
                int intensityMax = 256; // and < this number
                int rngMax = intensityMax - intensityMin;

                int r = siteRand.Next(rngMax) + intensityMin;
                int g = siteRand.Next(rngMax) + intensityMin;
                int b = siteRand.Next(rngMax) + intensityMin;
                colors[i] = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            // make a new image
            using var image = new Image<Rgba32>(width, height);

            // find out which site each pixel is closest to
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // normalize coordinates so (0, 0) is in the center of the image
                    int pixelX = x + width / 2;
                    int pixelY = y + height / 2;

                    // get the index of the closest site
                    double smallestDist = double.MaxValue;
                    int closestSiteIdx = 0;
                    for (int i = 0; i < siteCount; i++)
                    {
                        Point site = sites[i];

                        // normalize again
                        int siteX = site.X + width / 2;
                        int siteY = site.Y + height / 2;

                        double distance = metric(pixelX, pixelY, siteX, siteY);
                        if (distance < smallestDist)
                        {
                            smallestDist = distance;
                            closestSiteIdx = i;
                        }
                    }
                    // assign the site's color to the pixel
                    image[x, y] = colors[closestSiteIdx];
                }
            }

            if(drawSites)
            {
                // draw a black dot at every site
                for (int i = 0; i < siteCount; i++)
                {
                    Point site = sites[i];
                    int siteX = site.X;
                    int siteY = site.Y;

                    // as the image gets bigger, so should the dots, so they are easier to see while zoomed out.
                    int dotRadius = (int)Math.Ceiling(Math.Sqrt(width * height) / 300);
                    // for each pixel in a box of radius 'dotRadius' centered at the site
                    for (int x = siteX - dotRadius; x <= siteX + dotRadius; x++)
                        for (int y = siteY - dotRadius; y <= siteY + dotRadius; y++)
                            if (x > 0 && x < width && y > 0 && y < height) // if the position is in the bounds of the image...
                                image[x, y] = Color.Black; // ...draw a black pixel
                }
            }

            // draw relevent text in the bottom left corner
            int fontSize = 15;
            FontCollection collection = new FontCollection();
            FontFamily family = collection.Install(FONT_PATH);
            Font font = family.CreateFont(fontSize);

            string text = "";
            text += "Sites: " + siteCount + "\n";
            text += "Resolution: " + width + ", " + height + "\n";
            text += "Metric: " + metric.Method.Name;

            int textHeight = fontSize * 3; // times 3 because there are 3 lines of text

            image.Mutate(x => x.DrawText(text, font, Color.Black, new PointF(0, height - textHeight)));

            // save the image
            image.SaveAsPng(path);
        }

        // common metrics
        static double Euclidian(int x1, int y1, int x2, int y2)
        {
            double deltaXSquared = Math.Pow(x2 - x1, 2);
            double deltaYSquared = Math.Pow(y2 - y1, 2);

            return Math.Sqrt(deltaXSquared + deltaYSquared);
        }
        static double Manhattan(int x1, int y1, int x2, int y2)
        {
            int distX = Math.Abs(x2 - x1);
            int distY = Math.Abs(y2 - y1);

            return distX + distY;
        }

        // I got these metrics from https://numerics.mathdotnet.com/Distance.html
        static double Canberra(int x1, int y1, int x2, int y2)
        {
            // if either coordinate is (0, 0), the distance is 1.0.
            if ((x1 == 0 && y1 == 0) || (x2 == 0 && y2 == 0))
                return 1.0;

            double distance = 0;
            distance += (double)Math.Abs(x1 - x2) / (double)(Math.Abs(x1) + Math.Abs(x2));
            distance += (double)Math.Abs(y1 - y2) / (double)(Math.Abs(y1) + Math.Abs(y2));

            return distance;
        }
        static double Minkowski(int x1, int y1, int x2, int y2)
        {
            double p = 3.0;

            double distance = 0;
            distance += Math.Pow(Math.Abs(x2 - x1), p);
            distance += Math.Pow(Math.Abs(y2 - y1), p);
            distance = Math.Pow(distance, 1.0 / p);
            return distance;
        }
        // I came up with this one
        static double FuzzyEuclidian(int x1, int y1, int x2, int y2)
        {
            /* fuzziness. generally this represents the pixel width of the fuzziness
             but in certain edge cases fuzziness can be greater (such as when two sites are very close to each other) */
            int fuzziness = 10;
            double distance = Euclidian(x1, y1, x2, y2);
            distance += fuzzyRand.NextDouble() * fuzziness;

            return distance;
        }
    }
}
