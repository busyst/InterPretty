[org 0x7C00]
[BITS 16]
section .data
  a:db 0
section .text
global _start
_start:
mov esp, 0x90000
mov ax, 0
mov ds, ax
mov es, ax
mov bx, 0x1000
mov ah, 2
mov al, 128
mov ch, 0
mov cl, 2
mov dh, 0
int 0x13
mov ah, 0x0E
mov byte [a], 76
mov al, byte [a]
int 0x10
mov byte [a], 111
mov al, byte [a]
int 0x10
mov byte [a], 97
mov al, byte [a]
int 0x10
mov byte [a], 100
mov al, byte [a]
int 0x10
mov byte [a], 13
mov al, byte [a]
int 0x10
mov byte [a], 10
mov al, byte [a]
int 0x10
cli
jmp 0x0000:0x1000
