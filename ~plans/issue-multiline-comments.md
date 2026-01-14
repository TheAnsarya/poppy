## Description
Implement C-style multi-line comments using /**/ syntax.

## Syntax
```asm
/* This is a
   multi-line comment
   spanning several lines */

lda #$00  /* inline block comment */ sta $2000

/*
 * Documentation style
 * comment block
 */
```

## Acceptance Criteria

- [ ] Parse /_..._/ multi-line comments
- [ ] Support comments spanning multiple lines
- [ ] Support inline block comments
- [ ] Proper line number tracking across comments
- [ ] Nested comments NOT supported (match C behavior)
- [ ] Unit tests for multi-line comments

## Implementation Notes

- Modify Lexer to detect /_and scan until_/
- Track line numbers through comment body
- Skip comment content entirely
- Error on unterminated comment

## Related
Enhances existing comment support (; single-line)
