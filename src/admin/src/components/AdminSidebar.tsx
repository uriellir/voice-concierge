import type { AdminSection, AdminSectionId } from "../types";

type AdminSidebarProps = {
  sections: AdminSection[];
  selectedSection: AdminSectionId;
  onSelectSection: (sectionId: AdminSectionId) => void;
};

export function AdminSidebar(props: AdminSidebarProps) {
  const { sections, selectedSection, onSelectSection } = props;

  return (
    <aside className="sidebar">
      <div className="sidebar-card">
        <p className="sidebar-title">Admin Navigation</p>
        <nav className="sidebar-nav" aria-label="Admin menu">
          {sections.map((section) => (
            <button
              key={section.id}
              type="button"
              className={section.id === selectedSection ? "nav-item active" : "nav-item"}
              onClick={() => onSelectSection(section.id)}
            >
              <span className="nav-item-eyebrow">{section.eyebrow}</span>
              <span className="nav-item-label">{section.label}</span>
            </button>
          ))}
        </nav>
      </div>
    </aside>
  );
}
