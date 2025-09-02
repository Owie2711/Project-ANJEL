"""
UI Components Module
Reusable UI components for the Scrcpy Controller application.
"""

import tkinter as tk
import customtkinter as ctk
from typing import Optional, Callable, List, Any


class UITheme:
    """UI Theme configuration for consistent styling"""
    
    # Colors
    BACKGROUND = "#f8fafc"
    CARD_BACKGROUND = "#f8f9fa"
    BORDER_COLOR = "#000000"
    TEXT_PRIMARY = "#000000"
    TEXT_SECONDARY = "#666666"
    SUCCESS_COLOR = "#059669"
    ERROR_COLOR = "#dc2626"
    WARNING_COLOR = "#d97706"
    BUTTON_PRIMARY = "#ffffff"
    BUTTON_HOVER = "#f0f0f0"
    
    # Spacing - Better balanced
    PADDING_SMALL = 4
    PADDING_MEDIUM = 7
    PADDING_LARGE = 10
    PADDING_XL = 12
    CORNER_RADIUS = 12
    BORDER_WIDTH = 2
    
    # Fonts - 20% larger for better readability
    FONT_FAMILY = "Segoe UI"
    FONT_SIZE_SMALL = 11  # 9 * 1.2 = 10.8 ≈ 11
    FONT_SIZE_NORMAL = 12  # 10 * 1.2 = 12
    FONT_SIZE_LARGE = 14  # 12 * 1.2 = 14.4 ≈ 14
    FONT_SIZE_TITLE = 17  # 14 * 1.2 = 16.8 ≈ 17


class BaseCard(ctk.CTkFrame):
    """Base card component with consistent styling"""
    
    def __init__(self, parent, title: Optional[str] = None, **kwargs):
        # Apply default styling
        card_kwargs = {
            'corner_radius': UITheme.CORNER_RADIUS,
            'border_width': UITheme.BORDER_WIDTH,
            'border_color': UITheme.BORDER_COLOR,
            'fg_color': UITheme.CARD_BACKGROUND
        }
        card_kwargs.update(kwargs)
        
        super().__init__(parent, **card_kwargs)
        self.grid_columnconfigure(0, weight=1)
        
        self.current_row = 0
        
        # Add title if provided
        if title:
            self.add_title(title)
    
    def add_title(self, title: str):
        """Add a title to the card"""
        title_label = ctk.CTkLabel(
            self, 
            text=title,
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_LARGE, weight="bold"),
            text_color=UITheme.TEXT_PRIMARY
        )
        title_label.grid(row=self.current_row, column=0, sticky="w", 
                        pady=(UITheme.PADDING_MEDIUM, UITheme.PADDING_SMALL), 
                        padx=UITheme.PADDING_LARGE)
        self.current_row += 1
        return title_label
    
    def add_content_frame(self, **kwargs) -> ctk.CTkFrame:
        """Add a content frame to the card"""
        # Filter kwargs to only include supported CTkFrame parameters with correct types
        frame_kwargs = {'fg_color': 'transparent'}
        
        # Override fg_color if provided
        if 'fg_color' in kwargs and isinstance(kwargs['fg_color'], str):
            frame_kwargs['fg_color'] = kwargs['fg_color']
        
        # Create frame with basic parameters to avoid type issues
        # Additional styling can be applied after creation if needed
        content_frame = ctk.CTkFrame(self, fg_color=frame_kwargs['fg_color'])  # type: ignore[misc]
        content_frame.grid(row=self.current_row, column=0, sticky="ew", 
                          pady=(0, UITheme.PADDING_SMALL), 
                          padx=UITheme.PADDING_LARGE)
        content_frame.grid_columnconfigure(0, weight=1)
        self.current_row += 1
        return content_frame


class StyledButton(ctk.CTkButton):
    """Styled button with consistent theming"""
    
    def __init__(self, parent, text: str, command: Optional[Callable] = None, 
                 button_type: str = "primary", **kwargs):
        
        # Button type styling
        if button_type == "primary":
            btn_kwargs = {
                'fg_color': UITheme.BUTTON_PRIMARY,
                'hover_color': UITheme.BUTTON_HOVER,
                'text_color': UITheme.TEXT_PRIMARY,
                'border_width': UITheme.BORDER_WIDTH,
                'border_color': UITheme.BORDER_COLOR
            }
        elif button_type == "danger":
            btn_kwargs = {
                'fg_color': UITheme.ERROR_COLOR,
                'hover_color': "#ff6666",
                'text_color': "#ffffff",
                'border_width': UITheme.BORDER_WIDTH,
                'border_color': UITheme.BORDER_COLOR
            }
        else:
            btn_kwargs = {}
        
        # Apply default font
        btn_kwargs.update({
            'font': ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL, weight="bold"),
            'command': command
        })
        btn_kwargs.update(kwargs)
        
        super().__init__(parent, text=text, **btn_kwargs)


class StyledEntry(ctk.CTkEntry):
    """Styled entry field with consistent theming"""
    
    def __init__(self, parent, **kwargs):
        entry_kwargs = {
            'border_width': UITheme.BORDER_WIDTH,
            'border_color': UITheme.BORDER_COLOR,
            'fg_color': "#ffffff",
            'text_color': UITheme.TEXT_PRIMARY,
            'font': ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL)
        }
        entry_kwargs.update(kwargs)
        
        super().__init__(parent, **entry_kwargs)


class StyledComboBox(ctk.CTkComboBox):
    """Styled combobox with consistent theming"""
    
    def __init__(self, parent, **kwargs):
        combo_kwargs = {
            'border_width': UITheme.BORDER_WIDTH,
            'border_color': UITheme.BORDER_COLOR,
            'fg_color': "#ffffff",
            'text_color': UITheme.TEXT_PRIMARY,
            'dropdown_fg_color': "#ffffff",
            'font': ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL),
            'justify': "left"
        }
        combo_kwargs.update(kwargs)
        
        super().__init__(parent, **combo_kwargs)


class StyledCheckBox(ctk.CTkCheckBox):
    """Styled checkbox with consistent theming"""
    
    def __init__(self, parent, text: str, **kwargs):
        checkbox_kwargs = {
            'font': ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL),
            'text_color': UITheme.TEXT_PRIMARY,
            'border_width': UITheme.BORDER_WIDTH,
            'border_color': UITheme.BORDER_COLOR,
            'fg_color': "#ffffff",
            'hover_color': UITheme.BUTTON_HOVER,
            'checkmark_color': UITheme.TEXT_PRIMARY
        }
        checkbox_kwargs.update(kwargs)
        
        super().__init__(parent, text=text, **checkbox_kwargs)


class StatusLabel(ctk.CTkLabel):
    """Status label with color-coded messaging"""
    
    def __init__(self, parent, **kwargs):
        label_kwargs = {
            'font': ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_SMALL),
            'text_color': UITheme.TEXT_SECONDARY
        }
        label_kwargs.update(kwargs)
        
        super().__init__(parent, **label_kwargs)
    
    def update_status(self, message: str, status_type: str = "info"):
        """Update status with appropriate color coding"""
        color_map = {
            "info": UITheme.TEXT_SECONDARY,
            "success": UITheme.SUCCESS_COLOR,
            "error": UITheme.ERROR_COLOR,
            "warning": UITheme.WARNING_COLOR
        }
        
        self.configure(text=message, text_color=color_map.get(status_type, UITheme.TEXT_SECONDARY))


class LabeledInput:
    """Helper class for creating labeled input fields"""
    
    def __init__(self, parent, label_text: str, input_type: str = "entry", **kwargs):
        self.parent = parent
        self.label_text = label_text
        self.input_type = input_type
        
        # Create label
        self.label = ctk.CTkLabel(
            parent,
            text=label_text,
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL),
            text_color=UITheme.TEXT_PRIMARY
        )
        
        # Create input widget based on type
        if input_type == "entry":
            self.widget = StyledEntry(parent, **kwargs)
        elif input_type == "combobox":
            self.widget = StyledComboBox(parent, **kwargs)
        elif input_type == "checkbox":
            self.widget = StyledCheckBox(parent, text=label_text, **kwargs)
            self.label = None  # Checkbox has its own label
        else:
            raise ValueError(f"Unsupported input type: {input_type}")
    
    def grid_horizontal(self, row: int, label_column: int = 0, widget_column: int = 1, 
                       label_sticky: str = "", widget_sticky: str = "", **grid_kwargs):
        """Grid the label and widget horizontally"""
        if self.label:
            self.label.grid(row=row, column=label_column, sticky=label_sticky, **grid_kwargs)
        self.widget.grid(row=row + (0 if self.label else 0), column=widget_column if self.label else label_column, 
                        sticky=widget_sticky, **grid_kwargs)
    
    def grid_vertical(self, start_row: int, column: int = 0, **grid_kwargs):
        """Grid the label and widget vertically"""
        current_row = start_row
        if self.label:
            self.label.grid(row=current_row, column=column, sticky="", **grid_kwargs)
            current_row += 1
        self.widget.grid(row=current_row, column=column, sticky="", **grid_kwargs)
        return current_row + 1


class HeaderSection(ctk.CTkFrame):
    """Header section with icon and title"""
    
    def __init__(self, parent, title: str, icon: str = "", **kwargs):
        frame_kwargs = {
            'height': 40,
            'corner_radius': 0,
            'fg_color': 'transparent'
        }
        frame_kwargs.update(kwargs)
        
        super().__init__(parent, **frame_kwargs)
        self.grid_propagate(False)
        self.grid_columnconfigure(0, weight=1)  # Center the content
        
        # Container for centered content
        content_frame = ctk.CTkFrame(self, fg_color='transparent')
        content_frame.grid(row=0, column=0)
        
        # Icon
        if icon:
            icon_label = ctk.CTkLabel(
                content_frame, 
                text=icon,
                font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=20)
            )
            icon_label.grid(row=0, column=0, sticky="", padx=(0, 8))
        
        # Title
        title_label = ctk.CTkLabel(
            content_frame, 
            text=title,
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_TITLE, weight="bold"),
            text_color=UITheme.TEXT_PRIMARY
        )
        title_column = 1 if icon else 0
        title_label.grid(row=0, column=title_column, sticky="")


def setup_theme():
    """Setup CustomTkinter theme"""
    ctk.set_appearance_mode("light")
    ctk.set_default_color_theme("blue")