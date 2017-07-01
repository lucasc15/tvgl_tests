using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Boolean_Operations;

namespace TvglTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string stlInputFile = @"C:\Users\curra\Documents\companies\rwdi\liver.stl";
            using (StreamReader reader = new StreamReader(stlInputFile))
            {
                Stopwatch stopWatch;
                List<TessellatedSolid> solids;

                stopWatch = new Stopwatch();
                stopWatch.Start();
                solids = STLFileData.Open(reader.BaseStream, stlInputFile, false);
                Console.WriteLine(string.Format("Time non-parallel: {0}", stopWatch.ElapsedMilliseconds.ToString()));

                int totalVertices = 0;
                int totalTriangles = 0;
                int totalEdges = 0;
                foreach(TessellatedSolid solid in solids)
                {
                    totalVertices += solid.NumberOfVertices;
                    totalTriangles += solid.NumberOfFaces;
                    totalEdges += solid.NumberOfEdges;
                }

                Console.WriteLine(string.Format("Vertices: {0} | Triangles: {1} | Edges: {2} | Solids: {3}", totalVertices, totalTriangles, totalEdges, solids.Count));

                PolygonalFace[] faces = solids[0].Faces;
                stopWatch.Reset();
                stopWatch.Start();
                List<PolygonalFace> adjacentFaces;
                foreach(PolygonalFace face in faces)
                {
                    adjacentFaces = face.AdjacentFaces;
                }
                Console.WriteLine(string.Format("Total time for adjacent: {0} | Avg: {1}", stopWatch.ElapsedMilliseconds.ToString(), (stopWatch.ElapsedMilliseconds / solids[0].NumberOfFaces).ToString()));

                Flat cutPlane = new Flat(new double[] { 1500, 1500, 100 }, new double[] { 0, 0, 1 });
                ContactData contactData;
                bool success = Slice.GetContactData(solids[0], cutPlane, out contactData);
                SolidContactData positiveContact = contactData.PositiveSideContactData.ToList()[0];
                double positiveArea = positiveContact.OnSideFaces.Sum(f => f.Area) + positiveContact.OnSideContactFaces.Sum(f => f.Area);
                SolidContactData negativeContact = contactData.NegativeSideContactData.ToList()[0];
                double negativeArea = negativeContact.OnSideFaces.Sum(f => f.Area) + negativeContact.OnSideContactFaces.Sum(f => f.Area);
                Console.WriteLine(string.Format("Solid Area: {0} | Neg Area: {1} | Pos Area: {2} | Diff: {3}", solids[0].SurfaceArea, positiveArea, negativeArea, positiveArea + negativeArea));
                Console.WriteLine(string.Format("Total pos: {0} | Total neg: {1}", positiveContact.AllFaces.Count, negativeContact.AllFaces.Count));

                List<TessellatedSolid> positiveSolids, negativeSolids;
                Slice.OnFlat(solids[0], cutPlane, out positiveSolids, out negativeSolids);
                Console.WriteLine(string.Format("Positive Solid Area: {0} | Negative Solid Area {1} ", positiveSolids[0].SurfaceArea, negativeSolids[0].SurfaceArea));

                int numberOfCuts = 100;
                double interval = (solids[0].ZMax - solids[0].ZMin) / numberOfCuts;
                double currentCutHeight = solids[0].ZMin + interval;
                stopWatch.Reset();
                stopWatch.Start();
                while(currentCutHeight < solids[0].ZMax)
                {
                    Flat cutPlaneTest = new Flat(new double[] { 1500, 1500, currentCutHeight }, new double[] { 0, 0, 1 });
                    success = Slice.GetContactData(solids[0], cutPlane, out contactData);

                    positiveContact = contactData.PositiveSideContactData.ToList()[0];
                    positiveArea = positiveContact.OnSideFaces.Sum(f => f.Area) + positiveContact.OnSideContactFaces.Sum(f => f.Area);
                    negativeContact = contactData.NegativeSideContactData.ToList()[0];
                    negativeArea = negativeContact.OnSideFaces.Sum(f => f.Area) + negativeContact.OnSideContactFaces.Sum(f => f.Area);

                    if (Math.Abs(solids[0].SurfaceArea - (positiveArea + negativeArea)) > 0.000005)
                    {
                        throw new ApplicationException("Areas do not match");
                    }
                    currentCutHeight += interval;
                }
                Console.WriteLine(string.Format("{0} cuts in {1} ms. Avg: {2} ms", numberOfCuts, stopWatch.ElapsedMilliseconds.ToString(), (stopWatch.ElapsedMilliseconds / numberOfCuts).ToString()));
                
                Console.ReadLine();
            }
        }
    }
}
