using System;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Test;

namespace iText.Forms {
    public class FormFieldFlatteningTest : ExtendedITextTest {
        public static readonly String sourceFolder = NUnit.Framework.TestContext.CurrentContext.TestDirectory + "/../../resources/itext/forms/FormFieldFlatteningTest/";

        public static readonly String destinationFolder = NUnit.Framework.TestContext.CurrentContext.TestDirectory
             + "/test/itext/forms/FormFieldFlatteningTest/";

        [NUnit.Framework.TestFixtureSetUp]
        public static void BeforeClass() {
            CreateDestinationFolder(destinationFolder);
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FormFlatteningTest01() {
            String srcFilename = sourceFolder + "formFlatteningSource.pdf";
            String filename = destinationFolder + "formFlatteningTest01.pdf";
            PdfDocument doc = new PdfDocument(new PdfReader(srcFilename), new PdfWriter(filename));
            PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, true);
            form.FlattenFields();
            doc.Close();
            CompareTool compareTool = new CompareTool();
            String errorMessage = compareTool.CompareByContent(filename, sourceFolder + "cmp_formFlatteningTest01.pdf"
                , destinationFolder, "diff_");
            if (errorMessage != null) {
                NUnit.Framework.Assert.Fail(errorMessage);
            }
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FormFlatteningTest_DefaultAppearanceGeneration_Rot0() {
            String srcFilePattern = "FormFlatteningDefaultAppearance_0.0_";
            String destPattern = "FormFlatteningDefaultAppearance_0.0_";
            for (float i = 0; i < 360; i += 90) {
                String src = sourceFolder + srcFilePattern + i + ".pdf";
                String dest = destinationFolder + destPattern + i + "_flattened.pdf";
                String cmp = sourceFolder + "cmp_" + srcFilePattern + i + ".pdf";
                PdfDocument doc = new PdfDocument(new PdfReader(src), new PdfWriter(dest));
                PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, true);
                foreach (PdfFormField field in form.GetFormFields().Values) {
                    field.SetValue("Test");
                }
                form.FlattenFields();
                doc.Close();
                CompareTool compareTool = new CompareTool();
                String errorMessage = compareTool.CompareByContent(dest, cmp, destinationFolder, "diff_");
                if (errorMessage != null) {
                    NUnit.Framework.Assert.Fail(errorMessage);
                }
            }
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FormFlatteningTest_DefaultAppearanceGeneration_Rot90() {
            String srcFilePattern = "FormFlatteningDefaultAppearance_90.0_";
            String destPattern = "FormFlatteningDefaultAppearance_90.0_";
            for (float i = 0; i < 360; i += 90) {
                String src = sourceFolder + srcFilePattern + i + ".pdf";
                String dest = destinationFolder + destPattern + i + "_flattened.pdf";
                String cmp = sourceFolder + "cmp_" + srcFilePattern + i + ".pdf";
                PdfDocument doc = new PdfDocument(new PdfReader(src), new PdfWriter(dest));
                PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, true);
                foreach (PdfFormField field in form.GetFormFields().Values) {
                    field.SetValue("Test");
                }
                form.FlattenFields();
                doc.Close();
                CompareTool compareTool = new CompareTool();
                String errorMessage = compareTool.CompareByContent(dest, cmp, destinationFolder, "diff_");
                if (errorMessage != null) {
                    NUnit.Framework.Assert.Fail(errorMessage);
                }
            }
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FormFlatteningTest_DefaultAppearanceGeneration_Rot180() {
            String srcFilePattern = "FormFlatteningDefaultAppearance_180.0_";
            String destPattern = "FormFlatteningDefaultAppearance_180.0_";
            for (float i = 0; i < 360; i += 90) {
                String src = sourceFolder + srcFilePattern + i + ".pdf";
                String dest = destinationFolder + destPattern + i + "_flattened.pdf";
                String cmp = sourceFolder + "cmp_" + srcFilePattern + i + ".pdf";
                PdfDocument doc = new PdfDocument(new PdfReader(src), new PdfWriter(dest));
                PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, true);
                foreach (PdfFormField field in form.GetFormFields().Values) {
                    field.SetValue("Test");
                }
                form.FlattenFields();
                doc.Close();
                CompareTool compareTool = new CompareTool();
                String errorMessage = compareTool.CompareByContent(dest, cmp, destinationFolder, "diff_");
                if (errorMessage != null) {
                    NUnit.Framework.Assert.Fail(errorMessage);
                }
            }
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FormFlatteningTest_DefaultAppearanceGeneration_Rot270() {
            String srcFilePattern = "FormFlatteningDefaultAppearance_270.0_";
            String destPattern = "FormFlatteningDefaultAppearance_270.0_";
            for (float i = 0; i < 360; i += 90) {
                String src = sourceFolder + srcFilePattern + i + ".pdf";
                String dest = destinationFolder + destPattern + i + "_flattened.pdf";
                String cmp = sourceFolder + "cmp_" + srcFilePattern + i + ".pdf";
                PdfDocument doc = new PdfDocument(new PdfReader(src), new PdfWriter(dest));
                PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, true);
                foreach (PdfFormField field in form.GetFormFields().Values) {
                    field.SetValue("Test");
                }
                form.FlattenFields();
                doc.Close();
                CompareTool compareTool = new CompareTool();
                String errorMessage = compareTool.CompareByContent(dest, cmp, destinationFolder, "diff_");
                if (errorMessage != null) {
                    NUnit.Framework.Assert.Fail(errorMessage);
                }
            }
        }
    }
}
