[BITS 16]
section .text
  global _start
_start:
	push bp
	mov word [bp-3],0
	Ret:

	mov bx, word [bp-3]
	mov al, [bx]
	; Shift right 4 bits to get the higher bits
	shr al, 4
	cmp al, 9
	jg ad2
	add al, 48
	jmp ae2
	ad2:
	add al, 55
	ae2:
	
	; Display lower 4 bits
	mov ah, 0x0E
	int 0x10
	
	; Higher 4 bits
	mov al, [bx]
	
	; Lower 4 bits
	and al, 00001111b
	cmp al, 9
	jg ad
	add al, 48
	jmp ae
	ad:
	add al, 55
	ae:
	
	; Display higher 4 bits
	mov ah, 0x0E
	int 0x10

	jo End
	jmp Ret
	End:

	mov ah, 0x0E
	mov al, 10
	int 0x10
	mov al, 13
	int 0x10
	mov al, 'E'
	int 0x10
	mov al, 'N'
	int 0x10
	mov al, 'D'
	int 0x10
	hlt

	pop bp
	ret
