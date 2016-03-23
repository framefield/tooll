// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.CodeDom.Compiler;
using Framefield.Core;


namespace Framefield.Tooll
{
    /**
     * This class handles the conversion between nested code sections marked by special comments.
     * It finds the sections within a code.
     * It extracts a section and unintends the code respectively
     * When changing the current section code, it merges in back into the complete code
     */
    public class CodeSectionManager
    {
        public string CompleteCode {
            get {
                return string.Join("\n", _lines);
            }
            set {
                _lines.Clear();
                foreach (var l in value.Split('\n')) {
                    _lines.Add(l);
                }
                UpdateSectionsFromLines();
            }
        }

        public int CodeIndex { get; set; }

        public int GetCodeSectionStartLine(string sectionId) {
            return _sectionsById.ContainsKey(sectionId) ? _sectionsById[sectionId].StartLine : -1;
        }

        public CompilerErrorCollection CompilerErrorCollection { get; set; }

        public CodeDefinition CodeDefinition { get; set; }

        public ObservableCollection<CodeSectionViewModel> CodeSections { get; set; }    // TabControl binds to this

        public CodeSectionManager() {
            CodeSections = new ObservableCollection<CodeSectionViewModel>();
        }

        public bool ReplaceCodeInsideSection(string sectionId, string code) {
            if(! _sectionsById.ContainsKey(sectionId))
                return false; // no code with the given Id found

            var cs = _sectionsById[sectionId];

            var updatedLines = _lines.GetRange(0, cs.StartLine);
            foreach (var newSectionLine in code.Split('\n')) {
                updatedLines.Add(cs.Indentation + newSectionLine);
            }

            if (cs.EndLine < _lines.Count) {
                updatedLines.AddRange(_lines.GetRange(cs.EndLine, _lines.Count- cs.EndLine));
            }
            _lines = updatedLines;

            UpdateSectionsFromLines();
            return true;
        }

        public int GetSectionIndendationSize(string sectionId)
        {
            if ( sectionId == null || !_sectionsById.ContainsKey(sectionId))
                return 0;

            return _sectionsById[sectionId].Indentation.Length;
        }

        public String GetSectionCode(string sectionId) {
            if (!_sectionsById.ContainsKey(sectionId)) {
                return null;
            }

            var cs = _sectionsById[sectionId];
            var sectionLines = new List<string>();

            foreach (var block in _lines.GetRange(cs.StartLine, cs.EndLine - cs.StartLine)) {
                sectionLines.Add(cs.Indentation.Length == 0 ? block
                                                            : block.Replace(cs.Indentation, ""));
            }
            return string.Join("\n", sectionLines);
        }

        #region internal methods
        private void UpdateSectionsFromLines() {
            Regex sectionStartPattern = new Regex(@"(\s*)\/\/\s*>>>\s*(.*?)\s*$");
            Regex sectionEndPattern =  new Regex(@"(\s*)\/\/\s*<<<\s*(.*?)\s*$");

            SortedDictionary<string, CodeSectionViewModel> named_sections = new SortedDictionary<string, CodeSectionViewModel>();

            for (var i = 0; i<_lines.Count; ++i) {
                var line = _lines[i];

                MatchCollection matches = sectionStartPattern.Matches(line);
                if (matches.Count == 1) {
                    var match = matches[0];
                    string indentation = match.Groups[1].Value;
                    string id = match.Groups[2].Value;
                    var newSection= new CodeSectionViewModel() {Id = id, StartLine = i+1, Indentation = indentation, EndLine = 0};
                    if (named_sections.ContainsKey(id)) {
                        Logger.Warn("Code section '{0}' is already defined.", id);
                    }
                    else {
                        named_sections[id] = newSection;
                    }
                }

                matches = sectionEndPattern.Matches(line);
                if (matches.Count == 1) {
                    var match = matches[0];
                    string indentation = match.Groups[1].Value;
                    string id = match.Groups[2].Value;
                    if (named_sections.ContainsKey(id)) {
                        if (named_sections[id].EndLine != 0) {
                            Logger.Warn("Code section '{0}' has been closed multiple times.", id);
                        }
                        named_sections[id].EndLine = i;
                    }
                    else {
                        Logger.Info("Ignoring unmatched end of code section '{0}'.", id);
                    }
                }
            }

            // FIXME: This results is an upsorted list. The results should be sorted by line number
            foreach (var pair in named_sections)
            {
                var cs = pair.Value;
                if (cs.EndLine == 0)
                {
                    Logger.Info("Ignoring code section '{0}' without end.", cs.Id);
                    if (CodeSectionsContainID(pair.Key))
                        RemoveSection(pair.Key);
                }
                else
                {
                    if (!CodeSectionsContainID(pair.Key))
                    {
                        CodeSections.Add(cs);
                    }
                    _sectionsById[cs.Id] = cs;
                }
            }
            if (!CodeSectionsContainID(_routeSectionID))
            {
                var complete = new CodeSectionViewModel {Id = _routeSectionID, EndLine = _lines.Count, StartLine = 0, Indentation = ""};
                CodeSections.Add(complete);
                _sectionsById[_routeSectionID] = complete;
            }
            else
            {
                _sectionsById[_routeSectionID].EndLine = _lines.Count;
            }
            var unusedIDs = GetUnusedIDs(named_sections);
            foreach (var unusedID in unusedIDs)
            {
                RemoveSection(unusedID);
            }
        }

        private void RemoveSection(string unusedID)
        {
            _sectionsById.Remove(unusedID);
            var sectionToRemove = (from section in CodeSections
                                   where section.Id == unusedID
                                   select section).SingleOrDefault();
            CodeSections.Remove(sectionToRemove);
        }

        private string[] GetUnusedIDs(SortedDictionary<string, CodeSectionViewModel> namedSections)
        {
            return (from id in _sectionsById.Keys
                   where id != _routeSectionID && !namedSections.ContainsKey(id)
                   select id).ToArray();
        }

        private bool CodeSectionsContainID(string id)
        {
            return CodeSections.Any(section => section.Id == id);
        }

        #endregion

        private List<string> _lines = new List<string>();
        private readonly SortedList<string, CodeSectionViewModel> _sectionsById = new SortedList<string, CodeSectionViewModel>();
        private string _routeSectionID = "*";
    }
}
