[org 0x7C00]
[BITS 16]
section .data
  a dd 0
section .text
global _start
_start:
mov ax, 0x9000
mov ss, ax
mov sp, 0x8000
mov ax, 0
mov ds, ax
mov es, ax
mov bx, 0x1000
mov ah, 2
mov al, 1
mov ch, 0
mov cl, 2
mov dh, 0
int 0x13
mov dword [a],76
mov ah, 0x0E
mov al, [a]
int 0x10
mov dword [a],111
mov ah, 0x0E
mov al, [a]
int 0x10
mov dword [a],97
mov ah, 0x0E
mov al, [a]
int 0x10
mov dword [a],100
mov ah, 0x0E
mov al, [a]
int 0x10
mov dword [a],13
mov ah, 0x0E
mov al, [a]
int 0x10
mov dword [a],10
mov ah, 0x0E
mov al, [a]
int 0x10
jmp 0x0000:0x1000

times 510-($-$$) db 0
dw 0xAA55
