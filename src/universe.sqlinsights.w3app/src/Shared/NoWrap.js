import {makeStyles} from "@material-ui/core/styles";

const useStyles = makeStyles((theme) => ({
    noWrap: {
        whiteSpace: "nowrap",
    },
}));

export default function NoWrap(props) {
    const classes = useStyles();
    const className = props.className ?? "";
    return (
        <span className={`${classes.noWrap} ${className}`} style={props.style}>{props.children}</span>
    );
}
