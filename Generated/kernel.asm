section .data
  a dd 0
  b dd 0
section .text
global _start
_start:
mov dword [a],65
push word 122
push word 1
pop bx
pop ax
add ax,bx
mov [b],ax
return:
mov ah, 0x0E
mov al, [a]
int 0x10
push word [a]
push word 1
pop bx
pop ax
add ax,bx
mov [a],ax
mov ax,[a]
mov bx,[b]
cmp ax,bx
jge _C0
jmp return
_C0:

