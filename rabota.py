"""Программа для структурирования хаотичного текста с номерами заказов.

Преобразует найденные последовательности цифр в формат ПРXXXXXX_VLH.
Результат разбивается на строки до 255 символов.
Кнопки перенесены наверх, добавлена кнопка копирования всего поля.
Иконка приложения постоянно находится в трее Windows.
"""

import re
import threading
import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageDraw
import pystray


class TextStructurerApp:
    """Главный класс приложения для структурирования номеров заказов."""

    def __init__(self, window_root):
        """Инициализация интерфейса, горячих клавиш и постоянного трея."""
        self.root = window_root
        self.root.title("Структуризатор заказов")
        self.root.geometry("550x450")

        # Перехват кнопки закрытия окна (крестика) — теперь просто скрываем
        self.root.protocol('WM_DELETE_WINDOW', self.hide_to_tray)

        # --- ВЕРХНЯЯ ПАНЕЛЬ ДЛЯ КНОПОК ---
        control_panel = tk.Frame(window_root)
        control_panel.pack(fill=tk.X, padx=10, pady=10)

        # Кнопка обработки (Слева)
        btn_process = tk.Button(
            control_panel,
            text="Обработать и копировать",
            command=self.process_text,
            bg="#4CAF50",
            fg="white",
            font=("Arial", 10, "bold")
        )
        btn_process.pack(side=tk.LEFT)

        # Кнопка копирования всего текста (Справа)
        btn_copy_all = tk.Button(
            control_panel,
            text="Скопировать всё",
            command=self.copy_all_text,
            bg="#2196F3",
            fg="white",
            font=("Arial", 10, "bold")
        )
        btn_copy_all.pack(side=tk.RIGHT)

        # --- ТЕКСТОВОЕ ПОЛЕ (Снизу) ---
        label = tk.Label(
            window_root,
            text="Вставьте текст с номерами заказов:",
            font=("Arial", 10)
        )
        label.pack(anchor=tk.W, padx=10, pady=(5, 0))

        self.text_area = tk.Text(window_root, wrap=tk.WORD, font=("Arial", 10))
        self.text_area.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        # Привязка горячих клавиш через физические коды клавиатуры Windows
        self.text_area.bind("<Control-KeyPress>", self.handle_control_keys)

        # --- ИНИЦИАЛИЗАЦИЯ ПОСТОЯННОГО ТРЕЯ ---
        self.icon = None
        self.setup_permanent_tray()

    def handle_control_keys(self, event):
        """Обрабатывает нажатия Ctrl+клавиша по их физическим кодам на клавиатуре."""
        if event.keycode == 86:    # V (М)
            self.text_area.event_generate("<<Paste>>")
            return "break"
        elif event.keycode == 67:  # C (С)
            self.text_area.event_generate("<<Copy>>")
            return "break"
        elif event.keycode == 65:  # A (Ф)
            self.text_area.event_generate("<<SelectAll>>")
            return "break"
        elif event.keycode == 88:  # X (Ч)
            self.text_area.event_generate("<<Cut>>")
            return "break"

    def process_text(self):
        """Извлекает числа, валидирует длину и разбивает на строки до 255 символов."""
        input_text = self.text_area.get("1.0", tk.END).strip()
        if not input_text:
            messagebox.showwarning("Внимание", "Поле ввода пусто")
            return

        all_numbers = re.findall(r'\d+', input_text)
        valid_orders = []
        errors = []

        for num in all_numbers:
            length = len(num)
            if length == 6:
                valid_orders.append(f"ПР{num}_VLH")
            else:
                errors.append(f"{num} (цифр: {length})")

        # Формируем строки по 255 символов максимум
        lines = []
        current_line_orders = []

        for order in valid_orders:
            if not current_line_orders:
                potential_len = len(order)
            else:
                potential_len = len(",".join(current_line_orders)) + 1 + len(order)

            if potential_len <= 255:
                current_line_orders.append(order)
            else:
                if current_line_orders:
                    lines.append(",".join(current_line_orders))
                current_line_orders = [order]

        if current_line_orders:
            lines.append(",".join(current_line_orders))

        orders_output = "\n".join(lines)
        result_text = orders_output

        if errors:
            if result_text:
                result_text += "\n\n"
            result_text += "Ошибки (не 6 цифр):\n" + "\n".join(errors)

        self.text_area.delete("1.0", tk.END)
        self.text_area.insert("1.0", result_text)

        if orders_output:
            self.root.clipboard_clear()
            self.root.clipboard_append(orders_output)

    def copy_all_text(self):
        """Копирует абсолютно всё содержимое текстового поля в буфер обмена."""
        all_text = self.text_area.get("1.0", tk.END).strip()
        if all_text:
            self.root.clipboard_clear()
            self.root.clipboard_append(all_text)
        else:
            messagebox.showwarning("Внимание", "Текстовое поле пустое, нечего копировать.")

    def setup_permanent_tray(self):
        """Создает иконку в системном трее, которая работает постоянно."""
        img = Image.new('RGB', (64, 64), color=(73, 109, 137))
        draw_context = ImageDraw.Draw(img)
        draw_context.rectangle([(16, 16), (48, 48)], fill=(255, 255, 255))

        menu = (
            pystray.MenuItem('Показать окно', self.show_window, default=True),
            pystray.MenuItem('Выход', self.exit_app)
        )

        self.icon = pystray.Icon(
            "OrderStructurer",
            img,
            "Структуризатор заказов",
            menu
        )
        # Запуск иконки в фоновом потоке один раз при старте
        threading.Thread(target=self.icon.run, daemon=True).start()

    def hide_to_tray(self):
        """Скрывает окно с экрана (иконка в трее остается активной)."""
        self.root.withdraw()

    def show_window(self, item=None):  # pylint: disable=unused-argument
        """Просто делает скрытое окно видимым, не трогая иконку в трее."""
        self.root.after(0, self.root.deiconify)

    def exit_app(self, item=None):  # pylint: disable=unused-argument
        """Полностью закрывает приложение и удаляет иконку из трея."""
        if self.icon:
            self.icon.stop()
        self.root.after(0, self.root.destroy)


if __name__ == "__main__":
    main_root = tk.Tk()
    app = TextStructurerApp(main_root)
    main_root.mainloop()
