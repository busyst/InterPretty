[BITS 16]
section .data
  Console_ch:db 0
section .text
global _start
_start:
mov [Console_ch], 0
mov ah,0x0E
mov Console_ch, 84
mov al,[Console_ch]
inter 0x10
mov Console_ch, 111
mov al,[Console_ch]
inter 0x10
mov ah,0
inter 0x16
mov ah,0x0E
mov Console_ch, 111
mov al,[Console_ch]
inter 0x10
mov Console_ch, 107
mov al,[Console_ch]
inter 0x10
mov ah,0
inter 0x16
