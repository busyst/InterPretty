[BITS 16]
section .data
  x:dw 0
  y:dw 0
  c:db 0
  a:db 0
section .text
global _start
_start:
mov word [x], 0
mov word [y], 0
mov byte [c], 12
mov ah, 0x0E
mov byte [a], 84
mov al, byte [a]
int 0x10
mov byte [a], 111
mov al, byte [a]
int 0x10
mov ah, 0
int 0x16
mov ah, 0x0E
mov byte [a], 111
mov al, byte [a]
int 0x10
mov byte [a], 107
mov al, byte [a]
int 0x10
mov ah, 0
int 0x16
mov ax, 0x0013
int 0x10
mov ax, 0x0C13
mov cx, [x]
mov dx, [y]
mov al, [c]
int 0x10
return:
push word [x]
push word 1
pop cx
pop bx
add bx,cx
mov [x], bx
mov bx, [x]
mov cx, 320
cmp bx,cx
jle _C0
mov word [x], 0
push word [y]
push word 1
pop cx
pop bx
add bx,cx
mov [y], bx
mov bx, [y]
mov cx, 200
cmp bx,cx
jle _C1
mov word [y], 0
_C1:
push word [c]
push word 1
pop cx
pop bx
add bx,cx
mov [c], bx
_C0:
mov ax, 0x0C13
mov cx, [x]
mov dx, [y]
mov al, [c]
int 0x10
jmp return
