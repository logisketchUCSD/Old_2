using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using System.IO;
using System.Windows.Forms;
using Fragmenter;
using ConverterXML;
using Recognizers;
using Featurefy;
using CircuitRec;

namespace Flow
{
    public class Flow
    {
        private Sketch.Sketch sketchHolder;
        private CircuitRec.CircuitRec cr;
        private readonly string domainpath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\digital_domain.txt";
        private readonly string tcrfGateNongate = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\Gate_Nongate_Latest.tcrf";
        private readonly string tcrfWireLabel = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\Wire_Label_Latest.tcrf";
        private readonly string labelFileGateNongate = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\Gate_Nongate.txt";
        private readonly string labelFileWireLabel = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\Wire_Label.txt";
        private readonly string modelFileCRF = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\WireLabel.model";
        private readonly string modelFile = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\symbols.model";
        private readonly string AND = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\and.amat";
        private readonly string NAND = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\nand.amat";
        private readonly string NOR = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\nor.amat";
        private readonly string NOT = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\not.amat";
        private readonly string OR = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\or.amat";


        public Flow(Sketch.Sketch sketch)
        {
            sketchHolder = sketch;
            doCRF();
            doGrouper();
            //doCircuitRec();   
        }

        //private void doCircuitRec()
        //{
        //    // Create the domain file, here the digital domain
        //    Domain domain = new Domain(domainpath);
        //    this.cr = new CircuitRec.CircuitRec(domain, new Microsoft.Ink.WordList);

        //    // TODO: Need to refragment the wires more intelligently to make sure that there are only two endpoints in each wire
        //    List<Shape> toBeRemoved = new List<Shape>();
        //    List<Shape> toBeAdded = new List<Shape>();
        //    foreach (Shape shape in sketchHolder.ShapesL)
        //    {
        //        bool remove = false;
        //        if (shape.XmlAttrs.Type == "Wire")
        //        {
        //            foreach (Substroke sub in shape.SubstrokesL)
        //            {
        //                List<Substroke> listSub = new List<Substroke>();
        //                listSub.Add(sub);
        //                toBeAdded.Add(new Shape(new List<Shape>(), listSub, shape.XmlAttrs));
        //                if (!toBeRemoved.Contains(shape))
        //                    remove = true;
        //            }
        //        }
        //        if (remove)
        //            toBeRemoved.Add(shape);
        //    }

        //    sketchHolder.AddShapes(toBeAdded);
        //    sketchHolder.RemoveShapes(toBeRemoved);

        //    // The CircuitRec object holds the recognition results
        //    cr.Run(sketchHolder);
        //}

        private void doGrouper()
        {
            Grouper.Grouper group = new Grouper.Grouper(this.sketchHolder, 
                Grouper.Algorithm.NAIVE_H, Grouper.Algorithm.NAIVE_H);

            group.group();

            group.search();
        }

        private void doCRF()
        {
            //ConverterXML.MakeXML test = new MakeXML(sketchHolder);
            //test.WriteXML(@"c:\first.xml");
            int numLabels = 2;

            cleanUpSketch(ref sketchHolder, "AND", "OR", "NOT", "NAND", "NOR", "XOR", "XNOR",
                                            "Wire", "Other", "Label", "Nonlabel", "Nongate",
                                            "Nonwire", "BUBBLE", "Gate");

            Fragment.fragmentSketch(sketchHolder);

            CRF.SiteFeatures.setStageNumber(1);
            CRF.InteractionFeatures.setStageNumber(1);
            CRF.CRF crfGNG = new CRF.CRF(tcrfGateNongate, false);

            CRF.SiteFeatures.setStageNumber(2);
            CRF.InteractionFeatures.setStageNumber(2);
            CRF.CRF crfWL = new CRF.CRF(tcrfWireLabel, false);

            StreamReader labelReaderGNG = new StreamReader(labelFileGateNongate);
            Dictionary<int, string> intToStringTableGNG = new Dictionary<int, string>(numLabels);
            StreamReader labelReaderWL = new StreamReader(labelFileWireLabel);
            Dictionary<int, string> intToStringTableWL = new Dictionary<int, string>(numLabels);

            for (int i = 0; i < numLabels; i++)
            {
                string textLabelGNG = labelReaderGNG.ReadLine();
                int intLabelGNG = Convert.ToInt32(labelReaderGNG.ReadLine());
                
                string textLabelWL = labelReaderWL.ReadLine();
                int intLabelWL = Convert.ToInt32(labelReaderWL.ReadLine());
                
                intToStringTableGNG.Add(intLabelGNG, textLabelGNG);
                intToStringTableWL.Add(intLabelWL, textLabelWL);
            }

            CRF.SiteFeatures.setStageNumber(1);
            CRF.InteractionFeatures.setStageNumber(1);
            doLabeling(1, crfGNG, ref sketchHolder, intToStringTableGNG);
            
            cleanUpSketch(ref sketchHolder, "Nongate");

            bool useSVMnotCRF = false; // IMPORTANT: this variable should be set to
                                       // after we are done debugging since the best
                                       // accuracy was obtained using the svm. 

            if (useSVMnotCRF)
            {
                doSvmLabeling(ref sketchHolder);
            }
            else
            {
                CRF.SiteFeatures.setStageNumber(2);
                CRF.InteractionFeatures.setStageNumber(2);
                doLabeling(2, crfWL, ref sketchHolder, intToStringTableWL);
            }

        }

        /// <summary>
        /// Removes shapes with given types from Sketch.Sketch.
        /// </summary>
        /// <param name="sketchHolder">Sketch to be changed.</param>
        /// <param name="typesArray">Types which we want to remove.</param>
        private static void cleanUpSketch(ref Sketch.Sketch sketchHolder, params string[] typesArray)
        {
            List<string> typesAList = new List<string>(typesArray);

            foreach (Sketch.Shape shape in sketchHolder.Shapes)
            {
                String type = shape.XmlAttrs.Type.ToString();

                if (typesAList.Contains(type))
                {
                    sketchHolder.RemoveShape(shape);
                }
            }
        }
        /// <summary>
        /// Does the labeling of a sketch using SVM.
        /// </summary>
        /// <param name="sketchHolder">sketch to be changed</param>
        private void doSvmLabeling(ref Sketch.Sketch sketchHolder)
        {
            Substroke[] allSubstrokes = sketchHolder.Substrokes;
            int substrCount = allSubstrokes.Length;
            Recognizers.WGLRecognizer wglr = new Recognizers.WGLRecognizer(modelFileCRF);
            double[] boundBox = Recognizers.FeatureFunctions.bbox(sketchHolder);

            for (int i = 0; i < substrCount; ++i)
            {
                // The two lines below should be modified if we change the classification.
                string typeOfElement = "Gate";
                const int NUM_LABELS = 2;

                Substroke subStr = allSubstrokes[i];

                if (!subStr.FirstLabel.Equals(typeOfElement))
                {
                    Recognizers.Results res = wglr.Recognize(subStr, boundBox, NUM_LABELS);
                    string label = res.BestLabel;
                    double prob = res.BestMeasure;

                    sketchHolder.AddLabel(subStr, label, prob);
                }
            }
        }

        /// <summary>
        /// Does the labeling of a sketch using CRF.
        /// </summary>
        /// <param name="runNum">1 for Gate vs. Nongate stage, 2 for Wire vs. Label stage</param>
        /// <param name="crf">CRF.CRF used.</param>
        /// <param name="sketchHolder">Sketch to be labeled.</param>
        /// <param name="intToStringTable">Dictionary which returns a label given an int.</param>
        private static void doLabeling(int runNum, CRF.CRF crf, ref Sketch.Sketch sketchHolder, 
            Dictionary<int, string> intToStringTable)
        {
            List<Substroke> substrToLabel = new List<Substroke>();

            if (runNum == 1)
            {
                FeatureSketch fSketch =new FeatureSketch(ref sketchHolder);
                crf.initGraph(sketchHolder.Substrokes, ref fSketch);
            }

            if (runNum == 2)
            {
                Substroke[] allSubstrokes = sketchHolder.Substrokes;
                int substrCount = allSubstrokes.Length;

                for (int i = 0; i < substrCount; ++i)
                {
                    string typeOfElement = "Gate";

                    if (!allSubstrokes[i].FirstLabel.Equals(typeOfElement))
                        substrToLabel.Add(allSubstrokes[i]);
                }
                FeatureSketch fSketch = new FeatureSketch(ref sketchHolder);
                crf.initGraph(substrToLabel.ToArray(), ref fSketch);
            }

            crf.calculateFeatures();
            crf.infer();

            int[] outIntLabels;
            double[] outProbLabels;
            crf.findLabels(out outIntLabels, out outProbLabels);

            int labelsCount = outIntLabels.Length;
            for (int i = 0; i < labelsCount; i++)
            {
                if (runNum == 1)
                    sketchHolder.AddLabel(sketchHolder.Substrokes[i], intToStringTable[outIntLabels[i]], outProbLabels[i]);
                
                if (runNum == 2)
                    sketchHolder.AddLabel(substrToLabel[i], intToStringTable[outIntLabels[i]], outProbLabels[i]);

            }
        }

        public CircuitRec.CircuitRec circuit
        {
            get
            {
                return this.cr;
            }
        }

        public Sketch.Sketch SketchHolder
        {
            get
            {
                return this.sketchHolder;
            }
        }
    }
}
