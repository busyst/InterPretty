[BITS 16]
section .data
  kernel_a:dd 0
  kernel_b:dd 0
section .text
global _start
_start:
	mov dword [kernel_a], 0x000
	mov dword [kernel_b], 0x100
