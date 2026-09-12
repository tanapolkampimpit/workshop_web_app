namespace TodoApi.Dtos;

public record TodoGetDto(
    int Id,  // uint = 4 bytes, unsigned integer, ไม่ติดลบ 
    string Titel,
    bool Iscampleted
);