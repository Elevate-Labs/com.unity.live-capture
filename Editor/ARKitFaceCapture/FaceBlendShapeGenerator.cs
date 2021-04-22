using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// Generates the fields for <see cref="FaceBlendShapePose"/> from each enum value in <see cref="FaceBlendShape"/>.
    /// </summary>
    class FaceBlendShapeGenerator : AssetPostprocessor
    {
        /// <summary>
        /// The file that when modified/imported will trigger file generation.
        /// </summary>
        static readonly string k_TriggerFile = $"{nameof(FaceBlendShapePose)}.cs";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith(k_TriggerFile))
                {
                    var sourceFile = new FileInfo(asset);
                    var outputDir = sourceFile.Directory.FullName;
                    Generate(outputDir);
                }
            }
        }

        static void Generate(string outputDirectory)
        {
            var generator = new StringBuilder();

            generator.AppendLine(
                $@"// <auto-generated>
// This file is generated by {nameof(FaceBlendShapeGenerator)}, do not modify manually
using System;

namespace {typeof(FaceBlendShapePose).Namespace}
{{
    partial struct {nameof(FaceBlendShapePose)}
    {{
        /// <summary>
        /// The number of supported blend shapes.
        /// </summary>
        public const int shapeCount = {FaceBlendShapePose.shapes.Length};
");

            foreach (var name in FaceBlendShapePose.shapes)
            {
                generator.AppendLine($"        /// <inheritdoc cref=\"{nameof(FaceBlendShape)}.{name}\"/>");
                generator.AppendLine($"        public float {name};");
            }

            generator.AppendLine($@"
        float GetValue(int index)
        {{
            switch (index)
            {{");

            foreach (var name in FaceBlendShapePose.shapes)
            {
                generator.AppendLine($"                case {(int)name}: return {name};");
            }

            generator.AppendLine(
                $@"            }}
            throw new IndexOutOfRangeException($""Blend shape index {{index}} out of valid range [0, {{shapeCount}}]."");
        }}

        void SetValue(int index, float value)
        {{
            switch (index)
            {{");

            foreach (var name in FaceBlendShapePose.shapes)
            {
                generator.AppendLine($"                case {(int)name}: {name} = value; return;");
            }

            generator.AppendLine(
                $@"            }}
            throw new IndexOutOfRangeException($""Blend shape index {{index}} out of valid range [0, {{shapeCount}}]."");
        }}

        /// <summary>
        /// Horizontally mirrors the face pose.
        /// </summary>
        /// <remarks>ARKit's ARKit's default blend shapes are set so that 'right' would be the right side when viewing from the front.</remarks>
        public void FlipHorizontally()
        {{");

            foreach (var name in FaceBlendShapePose.shapes)
            {
                var str = name.ToString();

                if (str.Contains("Left"))
                {
                    generator.AppendLine($"            var temp{name} = {name};");
                }
                else if (str.Contains("Right"))
                {
                    generator.AppendLine($"            var temp{name} = {name};");
                }
            }
            foreach (var name in FaceBlendShapePose.shapes)
            {
                var str = name.ToString();

                if (str.Contains("Left"))
                {
                    generator.AppendLine($"            {name} = temp{Enum.Parse(typeof(FaceBlendShape), str.Replace("Left", "Right"))};");
                }
                else if (str.Contains("Right"))
                {
                    generator.AppendLine($"            {name} = temp{Enum.Parse(typeof(FaceBlendShape), str.Replace("Right", "Left"))};");
                }
            }

            generator.AppendLine(
                $@"        }}
    }}
}}");

            // change to Unix line endings to avoid warning in Unity
            var generatedContents = generator.ToString().Replace("\r\n", "\n");

            var fileName = $"{nameof(FaceBlendShapePose)}Fields.cs";
            var filePath = Path.Combine(outputDirectory, fileName);

            // only write the file if anything has changed to avoid triggering file watchers
            if (File.Exists(filePath) && File.ReadAllText(filePath) == generatedContents)
                return;

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(filePath, generatedContents, Encoding.UTF8);
        }
    }
}