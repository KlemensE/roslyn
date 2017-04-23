﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class SerializableTaggedText
    {
        public string Tag;
        public string Text;

        public static SerializableTaggedText Dehydrate(TaggedText taggedText)
        {
            return new SerializableTaggedText { Tag = taggedText.Tag, Text = taggedText.Text };
        }

        public static SerializableTaggedText[] Dehydrate(ImmutableArray<TaggedText> array)
        {
            if (array.IsDefaultOrEmpty)
            {
                return null;
            }

            var result = new SerializableTaggedText[array.Length];
            int index = 0;
            foreach (var tt in array)
            {
                result[index] = Dehydrate(tt);
                index++;
            }

            return result;
        }

        public TaggedText Rehydrate()
            => new TaggedText(Tag, Text);

        public static ImmutableArray<TaggedText> Rehydrate(SerializableTaggedText[] array)
        {
            if (array == null)
            {
                return ImmutableArray<TaggedText>.Empty;
            }

            var result = ArrayBuilder<TaggedText>.GetInstance(array.Length);
            foreach (var tt in array)
            {
                result.Add(tt.Rehydrate());
            }

            return result.ToImmutableAndFree();
        }
    }

    internal class SerializableDocumentSpan
    {
        public DocumentId DocumentId;
        public TextSpan SourceSpan;
        public SerializableClassifiedSpansAndHighlightSpan ClassifiedSpansAndHighlightSpan;

        public static SerializableDocumentSpan Dehydrate(DocumentSpan documentSpan)
        {
            return new SerializableDocumentSpan
            {
                DocumentId = documentSpan.Document.Id,
                SourceSpan = documentSpan.SourceSpan,
                ClassifiedSpansAndHighlightSpan = Dehydrate(documentSpan.Properties),
            };
        }

        private static SerializableClassifiedSpansAndHighlightSpan Dehydrate(ImmutableDictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    if (kvp.Value is ClassifiedSpansAndHighlightSpan classifiedSpans)
                    {
                        return SerializableClassifiedSpansAndHighlightSpan.Dehydrate(classifiedSpans);
                    }
                }
            }

            return null;
        }

        private static bool TryConvert(string key, object value, out object converted)
        {
            if (value is ClassifiedSpansAndHighlightSpan classifiedSpans)
            {
                converted = SerializableClassifiedSpansAndHighlightSpan.Dehydrate(classifiedSpans);
                return true;
            }

            converted = null;
            return false;
        }

        public static SerializableDocumentSpan[] Dehydrate(ImmutableArray<DocumentSpan> documentSpans)
        {
            var result = new SerializableDocumentSpan[documentSpans.Length];
            int index = 0;
            foreach (var ds in documentSpans)
            {
                result[index] = Dehydrate(ds);
                index++;
            }

            return result;
        }

        public DocumentSpan Rehydrate(Solution solution)
        {
            var properties = Rehydrate(ClassifiedSpansAndHighlightSpan);
            return new DocumentSpan(solution.GetDocument(DocumentId), SourceSpan);
        }

        private static ImmutableDictionary<string, object> Rehydrate(SerializableClassifiedSpansAndHighlightSpan dehydrated)
        {
            if (dehydrated == null)
            {
                return null;
            }

            return ImmutableDictionary<string, object>.Empty.Add(
                FindUsages.ClassifiedSpansAndHighlightSpan.Key, dehydrated.Rehydrate());
        }

        public static ImmutableArray<DocumentSpan> Rehydrate(Solution solution, SerializableDocumentSpan[] array)
        {
            var result = ArrayBuilder<DocumentSpan>.GetInstance(array.Length);
            foreach (var ds in array)
            {
                result.Add(ds.Rehydrate(solution));
            }

            return result.ToImmutableAndFree();
        }
    }

    #region NavigateTo

    internal class SerializableNavigateToSearchResult
    {
        public string AdditionalInformation;

        public string Kind;
        public NavigateToMatchKind MatchKind;
        public bool IsCaseSensitive;
        public string Name;
        public TextSpan[] NameMatchSpans;
        public string SecondarySort;
        public string Summary;

        public SerializableNavigableItem NavigableItem;

        internal static SerializableNavigateToSearchResult Dehydrate(INavigateToSearchResult result)
        {
            return new SerializableNavigateToSearchResult
            {
                AdditionalInformation = result.AdditionalInformation,
                Kind = result.Kind,
                MatchKind = result.MatchKind,
                IsCaseSensitive = result.IsCaseSensitive,
                Name = result.Name,
                NameMatchSpans = result.NameMatchSpans.ToArray(),
                SecondarySort = result.SecondarySort,
                Summary = result.Summary,
                NavigableItem = SerializableNavigableItem.Dehydrate(result.NavigableItem)
            };
        }

        internal INavigateToSearchResult Rehydrate(Solution solution)
        {
            return new NavigateToSearchResult(
                AdditionalInformation, Kind, MatchKind, IsCaseSensitive,
                Name, NameMatchSpans.ToImmutableArray(),
                SecondarySort, Summary, NavigableItem.Rehydrate(solution));
        }

        private class NavigateToSearchResult : INavigateToSearchResult
        {
            public string AdditionalInformation { get; }
            public string Kind { get; }
            public NavigateToMatchKind MatchKind { get; }
            public bool IsCaseSensitive { get; }
            public string Name { get; }
            public ImmutableArray<TextSpan> NameMatchSpans { get; }
            public string SecondarySort { get; }
            public string Summary { get; }

            public INavigableItem NavigableItem { get; }

            public NavigateToSearchResult(
                string additionalInformation, string kind, NavigateToMatchKind matchKind,
                bool isCaseSensitive, string name, ImmutableArray<TextSpan> nameMatchSpans,
                string secondarySort, string summary, INavigableItem navigableItem)
            {
                AdditionalInformation = additionalInformation;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                Name = name;
                NameMatchSpans = nameMatchSpans;
                SecondarySort = secondarySort;
                Summary = summary;
                NavigableItem = navigableItem;
            }
        }
    }

    internal class SerializableNavigableItem
    {
        public Glyph Glyph;

        public SerializableTaggedText[] DisplayTaggedParts;

        public bool DisplayFileLocation;

        public bool IsImplicitlyDeclared;

        public DocumentId Document;
        public TextSpan SourceSpan;

        SerializableNavigableItem[] ChildItems;

        public static SerializableNavigableItem Dehydrate(INavigableItem item)
        {
            return new SerializableNavigableItem
            {
                Glyph = item.Glyph,
                DisplayTaggedParts = SerializableTaggedText.Dehydrate(item.DisplayTaggedParts),
                DisplayFileLocation = item.DisplayFileLocation,
                IsImplicitlyDeclared = item.IsImplicitlyDeclared,
                Document = item.Document.Id,
                SourceSpan = item.SourceSpan,
                ChildItems = SerializableNavigableItem.Dehydrate(item.ChildItems)
            };
        }

        private static SerializableNavigableItem[] Dehydrate(ImmutableArray<INavigableItem> childItems)
        {
            return childItems.Select(Dehydrate).ToArray();
        }

        public INavigableItem Rehydrate(Solution solution)
        {
            var childItems = ChildItems == null
                ? ImmutableArray<INavigableItem>.Empty
                : ChildItems.Select(c => c.Rehydrate(solution)).ToImmutableArray();
            return new NavigableItem(
                Glyph, DisplayTaggedParts.Select(p => p.Rehydrate()).ToImmutableArray(),
                DisplayFileLocation, IsImplicitlyDeclared,
                solution.GetDocument(Document),
                SourceSpan,
                childItems);
        }

        private class NavigableItem : INavigableItem
        {
            public Glyph Glyph { get; }
            public ImmutableArray<TaggedText> DisplayTaggedParts { get; }
            public bool DisplayFileLocation { get; }
            public bool IsImplicitlyDeclared { get; }

            public Document Document { get; }
            public TextSpan SourceSpan { get; }

            public ImmutableArray<INavigableItem> ChildItems { get; }

            public NavigableItem(
                Glyph glyph, ImmutableArray<TaggedText> displayTaggedParts,
                bool displayFileLocation, bool isImplicitlyDeclared, Document document, TextSpan sourceSpan, ImmutableArray<INavigableItem> childItems)
            {
                Glyph = glyph;
                DisplayTaggedParts = displayTaggedParts;
                DisplayFileLocation = displayFileLocation;
                IsImplicitlyDeclared = isImplicitlyDeclared;
                Document = document;
                SourceSpan = sourceSpan;
                ChildItems = childItems;
            }
        }
    }

    #endregion
}