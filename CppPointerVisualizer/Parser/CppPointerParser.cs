using System;
using System.Text.RegularExpressions;
using CppPointerVisualizer.Models;

namespace CppPointerVisualizer.Parser
{
    public class CppPointerRegexParser
    {
        private int _addressCounter = 0x1000;

        public MemoryState Parse(string code)
        {
            var state = new MemoryState();
            var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                ParseLine(trimmed, state);
            }

            return state;
        }

        private void ParseLine(string line, MemoryState state)
        {
            // Удаляем точку с запятой
            line = line.TrimEnd(';').Trim();

            // Паттерны для различных объявлений
            // 1. Простая переменная: int a = 42 или const int a = 42
            var varPattern = @"^(const\s+)?(\w+)\s+(\w+)\s*=\s*(.+)$";
            // 2. Указатель: int *p = &a или const int *p или int* const p
            var ptrPattern = @"^(const\s+)?(\w+)\s*(\*+)\s*(const\s+)?(\w+)\s*=\s*(.+)$";
            // 3. Ссылка: int &ref = a или const int &ref
            var refPattern = @"^(const\s+)?(\w+)\s*&\s*(\w+)\s*=\s*(.+)$";

            Match match;

            // Проверяем ссылку
            match = Regex.Match(line, refPattern);
            if (match.Success)
            {
                ParseReference(match, state);
                return;
            }

            // Проверяем указатель
            match = Regex.Match(line, ptrPattern);
            if (match.Success)
            {
                ParsePointer(match, state);
                return;
            }

            // Проверяем переменную
            match = Regex.Match(line, varPattern);
            if (match.Success)
            {
                ParseVariable(match, state);
                return;
            }
        }

        private void ParseVariable(Match match, MemoryState state)
        {
            bool isConst = !string.IsNullOrWhiteSpace(match.Groups[1].Value);
            string type = match.Groups[2].Value;
            string name = match.Groups[3].Value;
            string valueStr = match.Groups[4].Value.Trim();

            object value = ParseValue(valueStr);

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = value,
                ObjectType = MemoryObjectType.Variable,
                Address = GenerateAddress(),
                IsConst = isConst
            };

            state.Objects.Add(obj);
        }

        private void ParsePointer(Match match, MemoryState state)
        {
            bool isConst = !string.IsNullOrWhiteSpace(match.Groups[1].Value);
            string type = match.Groups[2].Value;
            string stars = match.Groups[3].Value;
            bool isPointerConst = !string.IsNullOrWhiteSpace(match.Groups[4].Value);
            string name = match.Groups[5].Value;
            string valueStr = match.Groups[6].Value.Trim();

            int pointerLevel = stars.Length;

            // Получаем адрес переменной, на которую указывает
            string? pointsTo = null;
            if (valueStr.StartsWith("&"))
            {
                string targetName = valueStr.Substring(1).Trim();
                var target = state.GetObjectByName(targetName);
                if (target != null)
                {
                    pointsTo = target.Address;
                }
            }
            else if (valueStr == "nullptr" || valueStr == "NULL" || valueStr == "0")
            {
                pointsTo = "nullptr";
            }

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = pointsTo,
                ObjectType = MemoryObjectType.Pointer,
                Address = GenerateAddress(),
                PointsTo = pointsTo,
                IsConst = isConst,
                IsPointerConst = isPointerConst,
                PointerLevel = pointerLevel
            };

            state.Objects.Add(obj);
        }

        private void ParseReference(Match match, MemoryState state)
        {
            bool isConst = !string.IsNullOrWhiteSpace(match.Groups[1].Value);
            string type = match.Groups[2].Value;
            string name = match.Groups[3].Value;
            string valueStr = match.Groups[4].Value.Trim();

            // Ссылка всегда указывает на существующую переменную
            var target = state.GetObjectByName(valueStr);
            string? pointsTo = target?.Address;

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = target?.Value,
                ObjectType = MemoryObjectType.Reference,
                Address = GenerateAddress(), // Ссылка имеет свой адрес, но указывает на другую переменную
                PointsTo = pointsTo,
                IsConst = isConst
            };

            state.Objects.Add(obj);
        }

        private object ParseValue(string valueStr)
        {
            if (int.TryParse(valueStr, out int intVal))
                return intVal;
            if (double.TryParse(valueStr, out double doubleVal))
                return doubleVal;
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
                return valueStr.Trim('"');

            return valueStr;
        }

        private string GenerateAddress()
        {
            string addr = $"0x{_addressCounter:X4}";
            _addressCounter += 4;
            return addr;
        }
    }
}
