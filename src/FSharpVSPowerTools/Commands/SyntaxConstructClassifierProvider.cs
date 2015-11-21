﻿using FSharpVSPowerTools.ProjectSystem;
using FSharpVSPowerTools.SyntaxColoring;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace FSharpVSPowerTools
{
    static class ClassificationTypes
    {
        public const string FSharpReferenceType = Constants.fsharpReferenceType;
        public const string FSharpValueType = Constants.fsharpValueType;
        public const string FSharpPatternCase = Constants.fsharpPatternCase;
        public const string FSharpFunction = Constants.fsharpFunction;
        public const string FSharpMutableVar = Constants.fsharpMutableVar;
        public const string FSharpQuotation = Constants.fsharpQuotation;
        public const string FSharpModule = Constants.fsharpModule;
        public const string FSharpUnused = Constants.fsharpUnused;
        public const string FSharpPrintf = Constants.fsharpPrintf;
        public const string FSharpEscaped = Constants.fsharpEscaped;
        public const string FSharpOperator = Constants.fsharpOperator;

        [Export]
        [Name(FSharpReferenceType)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpReferenceClassificationType = null;

        [Export]
        [Name(FSharpValueType)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpValueClassificationType = null;

        [Export]
        [Name(FSharpPatternCase)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpPatternCaseClassificationType = null;

        [Export]
        [Name(FSharpFunction)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpFunctionClassificationType = null;

        [Export]
        [Name(FSharpMutableVar)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpMutableVarClassificationType = null;
        
        [Export]
        [Name(FSharpQuotation)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpQuotationClassificationType = null;

        [Export]
        [Name(FSharpModule)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpModuleClassificationType = null;

        [Export]
        [Name(FSharpUnused)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpUnusedClassificationType = null;

        [Export]
        [Name(FSharpPrintf)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpPrintfClassificationType = null;

        [Export]
        [Name(FSharpEscaped)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpEscapedClassificationType = null;

        [Export]
        [Name(FSharpOperator)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition FSharpOperatorClassificationType = null;
    }

    [Export]
    public class ClassificationColorManager 
    {
        private readonly IDictionary<VisualStudioTheme, IDictionary<string, FontColor>> themeColors =
            new Dictionary<VisualStudioTheme, IDictionary<string, FontColor>>();

        private VisualStudioTheme lastTheme = VisualStudioTheme.Unknown;

        static readonly FontColor DefaultDarkFontColor = 
            new FontColor(Color.FromRgb(220, 220, 220), Color.FromRgb(30, 30, 30));

        static readonly FontColor DefaultLightFontColor = new FontColor(Colors.Black, Colors.White);

        public ClassificationColorManager()
        {
            // Light/Blue theme colors
            var lightAndBlueColors = new Dictionary<string, FontColor>
            {
                { ClassificationTypes.FSharpReferenceType, new FontColor(Color.FromRgb(43, 145, 175)) },
                { ClassificationTypes.FSharpValueType, new FontColor(Color.FromRgb(43, 145, 175)) },
                { ClassificationTypes.FSharpPatternCase, new FontColor(Colors.Black) },
                { ClassificationTypes.FSharpFunction, new FontColor(Colors.Black) },
                { ClassificationTypes.FSharpMutableVar, new FontColor(Colors.Black) },
                { ClassificationTypes.FSharpQuotation, new FontColor(background: Color.FromRgb(255, 242, 223)) },
                { ClassificationTypes.FSharpModule, new FontColor(Color.FromRgb(43, 145, 175)) },
                { ClassificationTypes.FSharpUnused, new FontColor(Color.FromRgb(157, 157, 157)) },
                { ClassificationTypes.FSharpPrintf, new FontColor(Color.FromRgb(43, 145, 175)) },
                { ClassificationTypes.FSharpEscaped, new FontColor(Color.FromRgb(255, 0, 128)) },
                { ClassificationTypes.FSharpOperator, new FontColor(Colors.Black) }
            };

            themeColors.Add(VisualStudioTheme.Blue, lightAndBlueColors);
            themeColors.Add(VisualStudioTheme.Light, lightAndBlueColors);
            themeColors.Add(VisualStudioTheme.Unknown, lightAndBlueColors);

            // Dark theme colors
            var darkColors = new Dictionary<string, FontColor>
            {
                { ClassificationTypes.FSharpReferenceType, new FontColor(Color.FromRgb(78, 201, 176)) },
                { ClassificationTypes.FSharpValueType, new FontColor(Color.FromRgb(78, 201, 176)) },
                { ClassificationTypes.FSharpPatternCase, new FontColor(Color.FromRgb(220, 220, 220)) },
                { ClassificationTypes.FSharpFunction, new FontColor(Color.FromRgb(220, 220, 220)) },
                { ClassificationTypes.FSharpMutableVar, new FontColor(Color.FromRgb(220, 220, 220)) },
                { ClassificationTypes.FSharpQuotation, new FontColor(background: Color.FromRgb(98, 58, 0)) },
                { ClassificationTypes.FSharpModule, new FontColor(Color.FromRgb(78, 201, 176)) },
                { ClassificationTypes.FSharpUnused, new FontColor(Color.FromRgb(155, 155, 155)) },
                { ClassificationTypes.FSharpPrintf, new FontColor(Color.FromRgb(78, 220, 176)) },
                { ClassificationTypes.FSharpEscaped, new FontColor(Color.FromRgb(190, 0, 94)) },
                { ClassificationTypes.FSharpOperator, new FontColor(Color.FromRgb(220, 220, 220)) }
            };

            themeColors.Add(VisualStudioTheme.Dark, darkColors);
        }

        [Import]
        private ThemeManager themeManager = null;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService = null;

        [Import]
        private IClassificationTypeRegistryService classificationTypeRegistry = null;

        static FontColor GetDefaultFontColor(VisualStudioTheme theme)
        {
            return theme == VisualStudioTheme.Dark ? DefaultDarkFontColor : DefaultLightFontColor;
        }

        public FontColor GetDefaultColors(string category) 
        {
            var currentTheme = themeManager.GetCurrentTheme();
            var color = GetDefaultFontColor(currentTheme);
            if (!themeColors[currentTheme].TryGetValue(category, out color))
                LoggingModule.logWarningMessage(() => 
                    string.Format("Theme manager can't read colors correctly from {0} theme.", currentTheme));
            return color;
        }

        static SolidColorBrush ColorToBrush(Color? color)
        {
            return color == null ? null : new SolidColorBrush(color.Value);
        }

        static Color? GetBrushColor(Brush brush)
        {
            var solidBrush = brush as SolidColorBrush;
            return solidBrush == null ? (Color?) null : solidBrush.Color;
        } 

        public void UpdateColors()
        {
            var currentTheme = themeManager.GetCurrentTheme();

            if (currentTheme != VisualStudioTheme.Unknown && currentTheme != lastTheme)
            {
                lastTheme = currentTheme;
                var defaultColor = GetDefaultFontColor(currentTheme);
                var colors = themeColors[currentTheme];
                var formatMap = classificationFormatMapService.GetClassificationFormatMap(category: "text");
                    
                try
                {
                    formatMap.BeginBatchUpdate();
                    foreach (var pair in colors)
                    {
                        string type = pair.Key;
                        FontColor color = pair.Value;

                        var classificationType = classificationTypeRegistry.GetClassificationType(type);
                        var oldProp = formatMap.GetTextProperties(classificationType);

                        var foregroundBrush = 
                            GetNewBrush(oldProp.ForegroundBrush, color.Foreground, defaultColor.Foreground);

                        var backgroundBrush = 
                            GetNewBrush(oldProp.BackgroundBrush, color.Background, defaultColor.Background); 

                        var newProp = TextFormattingRunProperties.CreateTextFormattingRunProperties(
                            foregroundBrush, backgroundBrush, oldProp.Typeface, null, null, oldProp.TextDecorations, 
                            oldProp.TextEffects, oldProp.CultureInfo);

                        formatMap.SetTextProperties(classificationType, newProp);
                    }
                }
                finally
                {
                    formatMap.EndBatchUpdate();
                }
            }
        }

        SolidColorBrush GetNewBrush(Brush oldBrush, Color? defaultColor, Color? defaultThemeColor)
        {
            var oldColor = GetBrushColor(oldBrush);
            var newColor = oldColor != defaultThemeColor ? oldColor : defaultColor;
            return ColorToBrush(newColor);
        }
    }

    static class ClassificationFormats
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpReferenceType)]
        [Name(ClassificationTypes.FSharpReferenceType)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)] 
        internal sealed class FSharpReferenceTypeFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpReferenceTypeFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Types";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpReferenceType);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpValueType)]
        [Name(ClassificationTypes.FSharpValueType)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)] 
        internal sealed class FSharpValueTypeFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpValueTypeFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Value Types";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpValueType);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpPatternCase)]
        [Name(ClassificationTypes.FSharpPatternCase)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)] 
        internal sealed class FSharpPatternCaseFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpPatternCaseFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Patterns";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpPatternCase);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpFunction)]
        [Name(ClassificationTypes.FSharpFunction)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)] 
        internal sealed class FSharpFunctionFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpFunctionFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Functions / Methods";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpFunction);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpMutableVar)]
        [Name(ClassificationTypes.FSharpMutableVar)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)] 
        internal sealed class FSharpMutableVarFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpMutableVarFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Mutable Variables / Reference Cells";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpMutableVar);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpQuotation)]
        [Name(ClassificationTypes.FSharpQuotation)]
        [UserVisible(true)]
        [Order(Before = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpQuotationFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpQuotationFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Quotations";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpQuotation);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
                this.ForegroundCustomizable = false;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpModule)]
        [Name(ClassificationTypes.FSharpModule)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpModuleFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpModuleFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Modules";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpModule);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpUnused)]
        [Name(ClassificationTypes.FSharpUnused)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpUnusedFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public FSharpUnusedFormat(ClassificationColorManager colorManager)
            {
                this.DisplayName = "F# Unused Declarations";
                var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpUnused);
                this.ForegroundColor = colors.Foreground;
                this.BackgroundColor = colors.Background;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpPrintf)]
        [Name(ClassificationTypes.FSharpPrintf)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpPrintfFormat : ClassificationFormatDefinition
        {
             [ImportingConstructor]
             public FSharpPrintfFormat(ClassificationColorManager colorManager)
             {
                 this.DisplayName = "F# Printf Format";
                 var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpPrintf);
                 this.ForegroundColor = colors.Foreground;
                 this.BackgroundColor = colors.Background;
             }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpEscaped)]
        [Name(ClassificationTypes.FSharpEscaped)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpEscapedFormat : ClassificationFormatDefinition
        {
             [ImportingConstructor]
             public FSharpEscapedFormat(ClassificationColorManager colorManager)
             {
                 this.DisplayName = "F# Escaped Characters";
                 var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpEscaped);
                 this.ForegroundColor = colors.Foreground;
                 this.BackgroundColor = colors.Background;
             }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypes.FSharpOperator)]
        [Name(ClassificationTypes.FSharpOperator)]
        [UserVisible(true)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        internal sealed class FSharpOperatorFormat : ClassificationFormatDefinition
        {
             [ImportingConstructor]
             public FSharpOperatorFormat(ClassificationColorManager colorManager)
             {
                 this.DisplayName = "F# Operators";
                 var colors = colorManager.GetDefaultColors(ClassificationTypes.FSharpOperator);
                 this.ForegroundColor = colors.Foreground;
                 this.BackgroundColor = colors.Background;
             }
        }
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(UnusedDeclarationTag))]
    [Export(typeof(IClassifierProvider))]
    [ContentType("F#")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class SyntaxConstructClassifierProvider : ITaggerProvider, IClassifierProvider, IDisposable
    { 
        private readonly ShellEventListener _shellEventListener;
        private readonly ClassificationColorManager _classificationColorManager;
        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly VSLanguageService _fsharpVsLanguageService;
        private readonly ProjectFactory _projectFactory;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public SyntaxConstructClassifierProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ShellEventListener shellEventListener,
            ClassificationColorManager classificationColorManager,
            IClassificationTypeRegistryService classificationRegistry,
            ITextDocumentFactoryService textDocumentFactoryService,
            VSLanguageService fsharpVsLanguageService,
            ProjectFactory projectFactory)
        {
            _serviceProvider = serviceProvider;
            _classificationColorManager = classificationColorManager;
            _classificationRegistry = classificationRegistry;
            _textDocumentFactoryService = textDocumentFactoryService;
            _shellEventListener = shellEventListener;
            _fsharpVsLanguageService = fsharpVsLanguageService;
            _projectFactory = projectFactory;

            // Receive notification for Visual Studio theme change
            _shellEventListener.ThemeChanged += UpdateTheme;
        }

        private void UpdateTheme(object sender, EventArgs e)
        {
            _classificationColorManager.UpdateColors();
        }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            var generalOptions = Setting.getGeneralOptions(_serviceProvider);
            if (generalOptions == null || !generalOptions.SyntaxColoringEnabled) return null;

            bool includeUnusedReferences = generalOptions.UnusedReferencesEnabled;
            bool includeUnusedOpens = generalOptions.UnusedOpensEnabled;

            ITextDocument doc;
            if (_textDocumentFactoryService.TryGetTextDocument(buffer, out doc))
            {
                return buffer.Properties.GetOrCreateSingletonProperty(
                    () => new SyntaxConstructClassifier(doc, buffer, _classificationRegistry, _fsharpVsLanguageService,
                                    _serviceProvider, _projectFactory, includeUnusedReferences, includeUnusedOpens));
            }

            return null;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return GetClassifier(buffer) as ITagger<T>;
        }

        public void Dispose()
        {
            _shellEventListener.ThemeChanged -= UpdateTheme;
        }
    }
}
