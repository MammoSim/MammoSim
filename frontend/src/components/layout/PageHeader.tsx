import UserProfileSummary from "./UserProfileSummary";
import "../../styles/Header.css";

type PageHeaderProps = {
  title: string;
  description?: string;
  name: string;
  carrera: string;
  avatarColor?: string;
};

export default function PageHeader({
  title,
  description,
  name,
  carrera,
  avatarColor = "#FFC1DA",
}: PageHeaderProps) {
  return (
    <header className="header_container">
      <div style={{ flex: 1, minWidth: 0 }}>
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      <div className="profile_container">
        <UserProfileSummary
          name={name}
          carrera={carrera}
          avatarColor={avatarColor}
        />
      </div>
    </header>
  );
}
