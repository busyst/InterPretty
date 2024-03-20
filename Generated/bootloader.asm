[org 0x7C00]
[BITS 16]
section .text
global _start
_start:
    mov sp, 0xFFFF  ; Set stack pointer

    ; Set data segment registers
    mov ax, 0
    mov ds, ax
    mov es, ax

    ; Read sector from disk
    mov bx, 0x0200  ; Set buffer address
    mov ah, 2       ; BIOS read sector function
    mov al, 128     ; Number of sectors to read
    mov cx, 0       ; Cylinder number
    mov cl, 2       ; Sector number
    mov dh, 0       ; Head number
    int 0x13        ; BIOS interrupt

    ; Print "Load" using BIOS interrupt
    mov ah, 0x0E    ; BIOS teletype function
    mov al, 76 
    int 0x10        ; BIOS interrupt

    mov al, 111 
    int 0x10        ; BIOS interrupt

    mov al, byte 97
    int 0x10        ; BIOS interrupt

    mov al, byte 100
    int 0x10        ; BIOS interrupt

    mov al, 13 
    int 0x10        ; BIOS interrupt

    mov al, 10 
    int 0x10        ; BIOS interrupt

    cli             ; Clear interrupt flag
    jmp 0x0000:0x0200  ; Jump to bootloader loaded code
