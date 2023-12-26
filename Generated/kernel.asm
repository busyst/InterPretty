[BITS 16]
section .data
  kernel_chars db 107,121,115,0
section .text
  global _start
_start:
	mov ax, 0x07C0
	add ax, 0x20
	mov ds, ax
	
	mov cx, 3  
	
	mov ah, 0
	mov al, 3  ; Video mode 3 (80x25 text mode)
	int 0x10
	
	mov si, kernel_chars
	
	loop:
	mov ah, 0x0E
	mov al, [si]
	int 0x10
	
	inc si   ; Move to the next character in the array
	loop loop
