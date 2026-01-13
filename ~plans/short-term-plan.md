# 📋 Short-Term Plan - Poppy Compiler

**Period:** January 2026 (Weeks 1-4)
**Focus:** Project Foundation & Architecture

---

## 🎯 Week 1: Project Setup (Current)

### Goals
- [x] Establish project structure and configuration
- [x] Create documentation framework
- [x] Set up coding standards and guidelines
- [ ] Enable GitHub Issues and create Kanban board
- [ ] Create initial GitHub issues for planned work
- [ ] Git commit all initial setup work

### Tasks
1. ✅ Create `.github/copilot-instructions.md`
2. ✅ Update `README.md` with project overview
3. ✅ Update roadmap for compiler project
4. ✅ Create planning documents
5. ⬜ Enable GitHub Issues
6. ⬜ Create project Kanban board
7. ⬜ Create issues for Week 2-4 tasks
8. ⬜ Initial git commit with issue references

---

## 🎯 Week 2: Research & Architecture

### Goals
- [ ] Research existing assemblers (ASAR, XKAS, Ophis, ca65)
- [ ] Document instruction sets (6502, 65816)
- [ ] Design compiler architecture
- [ ] Choose implementation language

### Tasks
1. ⬜ Document 6502 instruction set reference
2. ⬜ Document 65816 instruction set reference
3. ⬜ Analyze ASAR syntax and features
4. ⬜ Analyze ca65 syntax and features
5. ⬜ Create architecture design document
6. ⬜ Evaluate C# vs Rust vs C++ for implementation
7. ⬜ Define Poppy assembly syntax specification
8. ⬜ Document hex notation (`$xxxx`) requirements

---

## 🎯 Week 3: Core Lexer & Parser

### Goals
- [ ] Implement basic lexer for Poppy syntax
- [ ] Implement basic parser structure
- [ ] Create token definitions
- [ ] Set up test framework

### Tasks
1. ⬜ Create `src/` project structure
2. ⬜ Define token types (opcodes, operands, labels, etc.)
3. ⬜ Implement lexer for basic assembly statements
4. ⬜ Implement parser for simple instructions
5. ⬜ Create unit test infrastructure
6. ⬜ Document lexer/parser design
7. ⬜ Handle lowercase opcodes requirement
8. ⬜ Handle `$` hex prefix parsing

---

## 🎯 Week 4: Basic Code Generation

### Goals
- [ ] Implement 6502 instruction encoding
- [ ] Create simple binary output
- [ ] Test with basic NES assembly

### Tasks
1. ⬜ Create opcode-to-byte mapping tables
2. ⬜ Implement addressing mode detection
3. ⬜ Generate binary output for basic instructions
4. ⬜ Create simple test programs
5. ⬜ Verify output against known-good assemblers
6. ⬜ Document instruction encoding

---

## 📊 Success Criteria

By end of Week 4:
- [ ] Can parse simple 6502 assembly files
- [ ] Can generate correct binary for basic instructions
- [ ] Have documented architecture and syntax spec
- [ ] GitHub Issues tracking all work
- [ ] All code committed with issue references

---

## 📝 Notes

- Keep everything documented as we go
- Commit frequently with issue references
- Use feature branches for significant work
- Update logs after each session

